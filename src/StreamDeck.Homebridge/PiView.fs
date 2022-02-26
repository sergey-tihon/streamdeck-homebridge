module StreamDeck.Homebridge.PiView

open Browser
open Elmish
open Fable.React
open Fable.React.Props
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel

type PiModel =
    { 
        IsDevMode: bool
        ReplyAgent: MailboxProcessor<PiOut_Events> option
        ServerInfo: Domain.GlobalSettings
        AuthInfo: Result<Client.AuthResult, string>

        IsLoading: Result<bool, string>
        Accessories: Map<string, Client.AccessoryDetails>
        SwitchAccessories: Map<string, Client.AccessoryDetails>
        RangeAccessories: Map<string, Client.AccessoryDetails>
        Layout: Client.RoomLayout[]
        
        ActionType: string option
        ActionSetting: Domain.ActionSetting
    }

type PiMsg =
    | PiConnected of startArgs:StartArgs * replyAgent:MailboxProcessor<PiOut_Events>
    | GlobalSettingsReceived of Domain.GlobalSettings
    | ActionSettingReceived of Domain.ActionSetting

    | UpdateServerInfo of Domain.GlobalSettings
    | Login of manual:bool
    | SetLoginResult of Result<Client.AuthResult,string>
    | Logout
    | GetData
    | SetData of Client.AccessoryDetails[] * Client.RoomLayout[]
    | ResetLoading of error: string
    | SelectActionType of actionType: string option
    | SelectAccessory of uniqueId: string option
    | SelectCharacteristic of characteristicType: string option
    | ChangeTargetValue of targetValue: float option
    | ToggleCharacteristic

let init isDevMode = fun () ->
    let state = {
        IsDevMode = isDevMode
        ReplyAgent = None
        ServerInfo = {
            Host = "http://192.168.0.55:8581"
            UserName = "admin"
            Password = "admin"
        }
        AuthInfo = Error null
        IsLoading = Ok false
        Accessories = Map.empty
        SwitchAccessories = Map.empty
        RangeAccessories = Map.empty
        Layout = [||]
        ActionType = None
        ActionSetting = {
            AccessoryId = None
            CharacteristicType = None
            TargetValue = None
        }
    }
    state, Cmd.none

let filterCharacteristics filter (accessory:Client.AccessoryDetails) =
    { 
        accessory with 
            serviceCharacteristics = 
                accessory.serviceCharacteristics
                |> Array.filter filter 
    }

let hasValue (ch:Client.AccessoryServiceCharacteristic) =
    match ch.value with
    | Some x when not (isNull x) -> true
    | _ -> false

let filterBoolCharacteristics =
    filterCharacteristics (fun x -> 
        x.canWrite && hasValue x // we can modify value
        && (x.format = "bool" // it is boolean
            || (x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1)) // or behave like boolean
    )

let filterRangeCharacteristics =
    filterCharacteristics (fun x ->
        if x.canWrite && hasValue x then
            match x.format, x.minValue, x.maxValue with
            | "uint8", Some a, Some b when b-a > 1 -> true
            | "float", Some a, Some b when b-a > 1 -> true
            | "int", Some a, Some b when b-a > 1 -> true
            | _ -> false
        else false
    )

let getCharacteristic characteristicType (accessory:Client.AccessoryDetails) =
    accessory.serviceCharacteristics
    |> Array.find (fun x -> x.``type`` = characteristicType)

let sdDispatch msg (model:PiModel) =
    match model.ReplyAgent with
    | Some(agent) -> agent.Post msg
    | None -> console.error("Message send before replyAgent assigned", msg)

let update (msg:PiMsg) (model:PiModel) =
    match msg with
    | PiConnected (startArgs, replyAgent) -> 
        let model' = { 
            model with 
                ReplyAgent = Some replyAgent
                ActionType = startArgs.ActionInfo |> Option.map (fun x -> x.action)
        }
        let model' = 
            startArgs.ActionInfo
            |> Option.map (fun x -> x.payload.settings)
            |> Option.bind (Domain.tryParse<Domain.ActionSetting>)
            |>  function
                | Some(x) -> { model' with ActionSetting = x }
                | None -> model'
        model', Cmd.none
    | GlobalSettingsReceived settings ->
        let model' = { 
            model with 
                ServerInfo = settings 
                AuthInfo = Error null
        }
        model', Cmd.ofMsg (Login false)
    | ActionSettingReceived settings ->
        let model' = { model with ActionSetting = settings }
        model', Cmd.none
    | UpdateServerInfo serverInfo ->
        let model' = { 
            model with 
                ServerInfo = serverInfo
                AuthInfo = Error null
        }
        model', Cmd.none
    | Login manual ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                let! result = Client.authenticate model.ServerInfo
                match manual, result with
                | true, Ok _ ->  model |> sdDispatch (PiOut_SetGlobalSettings model.ServerInfo)
                | _ -> ()
                dispatch <| SetLoginResult result
            } |> Async.StartImmediate
        { model with IsLoading = Ok true }, Cmd.ofSub delayedCmd
    | SetLoginResult authInfo ->
        let cmd = 
            match authInfo with
            | Ok _ -> Cmd.ofMsg GetData
            | _ -> Cmd.none
        let model' = { 
            model with 
                AuthInfo = authInfo
                IsLoading = Ok false
        }
        model', cmd
    | Logout ->
        { model with AuthInfo = Error null}, Cmd.none
    | GetData ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                match model.AuthInfo with 
                | Ok auth ->
                    let! layout= Client.getAccessoriesLayout model.ServerInfo.Host auth
                    let layout =
                        match layout with
                        | Ok layout -> layout
                        | Error err ->
                            GTag.logException err
                            [||]
                    let! accessories = Client.getAccessories model.ServerInfo.Host auth
                    
                    match accessories with
                    | Ok(accessories) ->
                        let devicesInLayout = 
                            layout
                            |> Array.collect (fun room -> 
                                room.services |> Array.map (fun x -> x.uniqueId))
                            |> Set.ofArray
                        let missingDevices =
                            accessories |> Array.filter (fun x -> devicesInLayout |> Set.contains x.uniqueId |> not)
                        let layout' = 
                            if missingDevices.Length = 0
                            then layout
                            else Array.append layout [|
                                {
                                    ``name`` = "Others"
                                    services =
                                        missingDevices
                                        |> Array.map (fun x->
                                            {
                                                uniqueId = x.uniqueId
                                                aid = x.aid
                                                iid = x.iid
                                                uuid = x.uuid
                                                customName = Some x.serviceName
                                            }
                                        )
                                }
                            |]

                        dispatch <| SetData (accessories, layout')
                    | Error e -> dispatch <| ResetLoading $"Cannot get list of accessories. {e}"
                | Error e -> dispatch <| ResetLoading $"User is not authenticated. {e}"
            } |> Async.StartImmediate
        { model with IsLoading = Ok true }, Cmd.ofSub delayedCmd
    | SetData (accessories, layout) ->
        let toMap (accessories:Client.AccessoryDetails[]) = 
            accessories |> Array.map (fun x-> x.uniqueId, x) |> Map.ofArray
        let state = { 
            model with
                Accessories = accessories |> toMap
                SwitchAccessories = accessories |> Array.map filterBoolCharacteristics |> toMap
                RangeAccessories = accessories |> Array.map filterRangeCharacteristics |> toMap
                Layout = layout
                IsLoading = Ok false
        }
        state, Cmd.none
    | ResetLoading error ->
        GTag.logException error
        { model with IsLoading = Error error}, Cmd.none
    | SelectActionType actionType -> 
        { model with ActionType = actionType}, Cmd.none
    | SelectAccessory uniqueId ->
        let model' = { 
            model with 
                ActionSetting = {
                    model.ActionSetting with 
                        AccessoryId = uniqueId
                        CharacteristicType = None
                        TargetValue = None
                }
        }
        model |> sdDispatch (PiOut_SetSettings model'.ActionSetting)
        model', Cmd.none
    | SelectCharacteristic characteristicType ->
        let targetValue =
            match characteristicType, model.ActionSetting.AccessoryId with
            | Some(characteristicType), Some(accessoryId) ->
                let ch = model.Accessories |> Map.find accessoryId |> getCharacteristic characteristicType
                Some (ch.value.Value :?> float)
            | _ -> None
        let model'= { 
            model with 
                ActionSetting = {
                    model.ActionSetting with 
                        CharacteristicType = characteristicType
                        TargetValue = targetValue
                } 
        }
        model |> sdDispatch (PiOut_SetSettings model'.ActionSetting)
        model', Cmd.none
    | ChangeTargetValue targetValue ->
        let model'= { 
            model with 
                ActionSetting = {
                    model.ActionSetting with 
                        TargetValue = targetValue
                } 
        }
        model |> sdDispatch (PiOut_SetSettings model'.ActionSetting)
        model', Cmd.none
    | ToggleCharacteristic ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            match model.AuthInfo, model.ActionSetting.AccessoryId, model.ActionSetting.CharacteristicType with
            | Ok(authInfo), Some(selectedAccessoryId), Some(characteristicType) ->
                match model.ActionType with
                | Some(Domain.SWITCH_ACTION_NAME) ->
                    async {
                        let! accessory = Client.getAccessory model.ServerInfo.Host authInfo selectedAccessoryId
                        match accessory with
                        | Ok accessory ->
                            let ch = accessory |> getCharacteristic characteristicType
                            let currentValue = ch.value.Value :?> int
                            let targetValue = 1 - currentValue
                            let! accessory' = Client.setAccessoryCharacteristic model.ServerInfo.Host authInfo selectedAccessoryId characteristicType targetValue
                            match accessory' with 
                            | Ok accessory' -> model |> sdDispatch (PiOut_SendToPlugin accessory')
                            | Error e -> console.error(e)
                        | Error e -> console.error(e)
                    } |> Async.StartImmediate
                | Some(Domain.SET_ACTION_NAME) -> 
                    async {
                        let targetValue = model.ActionSetting.TargetValue.Value
                        let! accessory' = Client.setAccessoryCharacteristic model.ServerInfo.Host authInfo selectedAccessoryId characteristicType targetValue
                        match accessory' with
                        | Ok accessory' -> model |> sdDispatch (PiOut_SendToPlugin accessory')
                        | Error e -> console.error(e)
                    } |> Async.StartImmediate
                | _ -> console.error("Unexpected action ", model.ActionType)
            | _ -> ()
        model, Cmd.ofSub delayedCmd

let view model dispatch =

    let accessorySelector (accessories:Map<string, Client.AccessoryDetails>) = [
        match model.ActionSetting.AccessoryId with
        | Some(uniqueId) when not <| accessories.ContainsKey(uniqueId) ->
            details [Class $"message caution"] [
                summary [Style [Color "red"]] [
                    str "Configured device is no longer available on Homebridge. Please update Homebridge or Reset button config."
                ]
            ]
            div [Class "sdpi-item"] [
                button [
                    Class "sdpi-item-value"; 
                    OnClick (fun _ -> dispatch <| SelectAccessory None)
                ] [ str "Reset button config" ]
            ]
        | _ ->
            div [Class "sdpi-item"] [
                div [Class "sdpi-item-label"] [str "Accessory"]
                select [
                    Class "sdpi-item-value select"
                    Value (model.ActionSetting.AccessoryId |> Option.defaultValue "DEFAULT")
                    OnChange (fun x -> 
                        let msg = if x.Value = "DEFAULT" then None else Some x.Value
                        dispatch <| SelectAccessory msg)
                ] [
                    option [Value "DEFAULT"] []
                    for room in model.Layout do
                        optgroup [Label room.name] [
                            yield! room.services
                            |> Array.choose (fun itemInfo -> 
                                Map.tryFind itemInfo.uniqueId accessories 
                                |> Option.map (fun accessoryDetails ->
                                    let name =  itemInfo.customName |> Option.defaultValue accessoryDetails.serviceName
                                    name, accessoryDetails
                                )
                            )
                            |> Array.sortBy fst
                            |> Array.map (fun (name, accessoryDetails) -> 
                                option [Value accessoryDetails.uniqueId; 
                                        Disabled (accessoryDetails.serviceCharacteristics.Length = 0)] [
                                    str name
                                ]
                            )
                        ]
                ]
            ]
    ]
    let characteristicSelector (accessory:Client.AccessoryDetails) =
        div [Class "sdpi-item"] [
            div [Class "sdpi-item-label"] [str "Characteristic"]
            select [
                Class "sdpi-item-value select"
                Value (model.ActionSetting.CharacteristicType |> Option.defaultValue "DEFAULT")
                OnChange (fun x -> 
                    let msg = if x.Value = "DEFAULT" then None else Some x.Value
                    dispatch <| SelectCharacteristic msg)
            ] [
                option [Value "DEFAULT"] []
                let characteristics = accessory.serviceCharacteristics |> Array.sortBy (fun x -> x.``type``)
                for x in characteristics do
                    option [Value x.``type``] [
                        str x.description
                    ]
            ]
        ]
    let testButton() =
        div [Class "sdpi-item"] [
            button [
                Class "sdpi-item-value"; 
                OnClick (fun _ -> dispatch <| ToggleCharacteristic)
            ] [
                str "Test configuration (Apply)"
            ]
        ]
    let message icon color message =
        details [Class $"message {icon}"] [
            summary [Style [Color color]] [
                str message
            ]
        ]
    let successConfirmation =
        message "" "green" "Button successfully configured"

    div [Class "sdpi-wrapper"] [
        match model.IsLoading with
        | Ok true -> message "info" "orange" "Waiting for Homebridge API response ..."
        | _ ->
            match model.IsLoading with
            | Error error -> message "caution" "red" error
            | _ -> ()

            match model.AuthInfo with
            | Error (error) ->
                if not <| System.String.IsNullOrEmpty(error) then 
                    message "caution" "red" error

                div [Class "sdpi-item"; Type "field"] [
                    div [Class "sdpi-item-label"] [str "Server"]
                    input [
                        Class "sdpi-item-value"
                        Value model.ServerInfo.Host
                        Placeholder "e.g. http://192.168.0.1:8581"
                        Required true
                        Pattern "^(.*:)//([A-Za-z0-9\-\.]+)(:[0-9]+)?$"
                        OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with Host = x.Value })
                    ]
                ]
                div [Class "sdpi-item"; Type "field"] [
                    div [Class "sdpi-item-label"] [str "UserName"]
                    input [
                        Class "sdpi-item-value"
                        Value model.ServerInfo.UserName
                        Required true
                        OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with UserName = x.Value })
                    ]
                ]
                div [Class "sdpi-item"; Type "password"] [
                    div [Class "sdpi-item-label"] [str "Password"]
                    input [
                        Class "sdpi-item-value"
                        Type "password"
                        Value model.ServerInfo.Password
                        Required true
                        OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with Password = x.Value })
                    ]
                ]
                div [Class "sdpi-item"; Type "button"] [
                    button [
                        Class "sdpi-item-value"; 
                        OnClick (fun _ -> dispatch <| Login true)
                    ] [ str "Login" ]
                ]
            | Ok _ ->
                match model.ActionType with
                | None ->
                    div [Class "sdpi-item"] [
                        div [Class "sdpi-item-label"] [str "Action Type"]
                        select [
                            Class "sdpi-item-value select"
                            Value (model.ActionType |> Option.defaultValue "DEFAULT")
                            OnChange (fun x -> 
                                let msg = if x.Value = "DEFAULT" then None else Some x.Value
                                dispatch <| SelectActionType msg)
                        ] [
                            option [Value "DEFAULT"] []
                            option [Value Domain.CONFIG_ACTION_NAME] [str "Config UI"]
                            option [Value Domain.SWITCH_ACTION_NAME] [str "Switch"]
                            option [Value Domain.SET_ACTION_NAME] [str "Set state"]
                        ]
                    ]
                | Some(Domain.CONFIG_ACTION_NAME) ->
                    successConfirmation
                | Some(Domain.SWITCH_ACTION_NAME) ->
                    yield! accessorySelector model.SwitchAccessories
                    match model.ActionSetting.AccessoryId with
                    | Some(uniqueId) when model.SwitchAccessories.ContainsKey uniqueId ->
                        let accessory = model.SwitchAccessories |> Map.find uniqueId
                        characteristicSelector accessory

                        match model.ActionSetting.CharacteristicType with
                        | Some characteristicType ->
                            if model.IsDevMode then testButton()
                            successConfirmation
                            //characteristicDetails characteristicType accessory
                        | None -> ()
                    | _ -> ()
                | Some(Domain.SET_ACTION_NAME) ->
                    yield! accessorySelector model.RangeAccessories
                    match model.ActionSetting.AccessoryId with
                    | Some(uniqueId) when model.RangeAccessories.ContainsKey uniqueId ->
                        let accessory = model.RangeAccessories |> Map.find uniqueId
                        characteristicSelector accessory

                        match model.ActionSetting.CharacteristicType, model.ActionSetting.TargetValue with
                        | Some characteristicType, Some targetValue ->
                            let ch = accessory |> getCharacteristic characteristicType
                            match ch.minValue, ch.minStep, ch.maxValue with
                            | Some minValue, Some minStep, Some maxValue ->
                                div [Type "range"; Class "sdpi-item"] [
                                    div [Class "sdpi-item-label"] [str $"Target value ({targetValue})"]
                                    div [Class "sdpi-item-value"] [
                                        span [Class "clickable"; Value minValue] [str $"{minValue}"]
                                        input [Type "range"; Min minValue; Max maxValue; Step minStep; Value targetValue;
                                            OnInput (fun x -> 
                                                let payload = Some(float x.Value)
                                                dispatch <| ChangeTargetValue payload)]
                                        span [Class "clickable"; Value maxValue] [str $"{maxValue}"]
                                    ]
                                ]

                                if model.IsDevMode then testButton()
                                successConfirmation
                            | _ -> ()
                        | _ -> ()
                    | _ -> ()
                | Some ty -> 
                    p [] [str $"Unsupported action type {ty}"]

                div [Class "sdpi-item"; Type "button"] [
                    button [
                        Class "sdpi-item-value"; 
                        OnClick (fun _ -> dispatch <| Logout)
                    ] [ str "Logout" ]
                ]
    ]
