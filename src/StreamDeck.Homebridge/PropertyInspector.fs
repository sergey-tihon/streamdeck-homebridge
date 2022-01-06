module StreamDeck.Homebridge.PI

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
        Accessories: Map<string, Client.AccessoryDetails>
        Layout: Client.RoomLayout[]
        ActionSetting: Domain.ToggleSetting
    }

type PiMsg =
    | PiConnected of startArgs:StartArgs * replyAgent:MailboxProcessor<PiOut_Events>
    | GlobalSettingsReceived of Domain.GlobalSettings
    | ActionSettingReceived of Domain.ToggleSetting

    | UpdateServerInfo of Domain.GlobalSettings
    | Login of manual:bool
    | SetLoginResult of Result<Client.AuthResult,string>
    | Logout
    | GetData
    | SetData of Client.AccessoryDetails[] * Client.RoomLayout[]
    | SelectedAccessory of uniqueId: string option
    | SelectedCharacteristic of characteristicType: string option
    | ToggleCharacteristic
    | UpdateAccessory of accessory: Client.AccessoryDetails

let init isDevMode = fun () ->
    let state = {
        IsDevMode = isDevMode
        ReplyAgent = None
        ServerInfo = {
            Host = "http://192.168.0.213:8581"
            UserName = "admin"
            Password = "admin"
        }
        AuthInfo = Error null
        Accessories = Map.empty
        Layout = [||]
        ActionSetting = {
            AccessoryId = None
            CharacteristicType = None
        }
    }
    state, Cmd.none //Cmd.ofMsg GetData

let filterBoolCharacteristics (accessory:Client.AccessoryDetails) =
    let characteristics =
        accessory.serviceCharacteristics
        |> Array.filter (fun x -> 
            x.canWrite // we can modify value
            && (x.format = "bool" // it is boolean
                || (x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1)) // or behave like boolean
        )
    { accessory with serviceCharacteristics = characteristics }

let getIntValue characteristicType (accessory:Client.AccessoryDetails) = 
    let characteristic = 
        accessory.serviceCharacteristics 
        |> Array.find (fun x -> x.``type`` = characteristicType)
    characteristic.value :?> int

let sdDispatch msg (model:PiModel) =
    match model.ReplyAgent with
    | Some(agent) -> agent.Post msg
    | None -> console.error("Message send before replyAgent assigned", msg)

let update (msg:PiMsg) (model:PiModel) =
    match msg with
    | PiConnected (startArgs, replyAgent) -> 
        let model' = { model with ReplyAgent = Some replyAgent}
        let model' = 
            startArgs.ActionInfo
            |> Option.map (fun x -> x.payload.settings)
            |> Option.bind (Domain.tryParse<Domain.ToggleSetting>)
            |>  function
                | Some(x) -> { model' with ActionSetting = x }
                | None -> model'
        model', Cmd.none
    | GlobalSettingsReceived settings ->
        let model'= { 
            model with 
                ServerInfo = settings 
                AuthInfo = Error null
        }
        model', Cmd.ofMsg (Login false)
    | ActionSettingReceived settings ->
        let model' = { model with ActionSetting = settings }
        model', Cmd.none
    | UpdateServerInfo serverInfo ->
        { model with ServerInfo = serverInfo}, Cmd.none
    | Login manual ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                let! result = Client.authenticate model.ServerInfo
                match manual, result with
                | true, Ok _ ->  model |> sdDispatch (PiOut_SetGlobalSettings model.ServerInfo)
                | _ -> ()
                dispatch <| SetLoginResult result
            } |> Async.StartImmediate
        model, Cmd.ofSub delayedCmd
    | SetLoginResult authInfo ->
        let cmd = 
            match authInfo with
            | Ok _ -> Cmd.ofMsg GetData
            | _ -> Cmd.none
        { model with AuthInfo = authInfo}, cmd
    | Logout ->
        { model with AuthInfo = Error null}, Cmd.none
    | GetData ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                match model.AuthInfo with 
                | Ok token ->
                    let! layout = Client.getAccessoriesLayout model.ServerInfo.Host token
                    let! data = Client.getAccessories model.ServerInfo.Host token
                    match data, layout with
                    | Some(data), Some(layout) ->
                        let accessories = 
                            data 
                            |> Array.choose (fun accessory ->
                                let accessory' = filterBoolCharacteristics accessory
                                if accessory'.serviceCharacteristics.Length > 0
                                then Some accessory' else None
                            )
                        dispatch <| SetData (accessories, layout)
                    | _ -> ()
                | _ -> ()
            } |> Async.StartImmediate
        model, Cmd.ofSub delayedCmd
    | SetData (accessories, layout) ->
        let state = { 
            model with
                Accessories = accessories |> Array.map (fun x-> x.uniqueId,x) |> Map.ofArray
                Layout = layout
        }
        state, Cmd.none
    | SelectedAccessory uniqueId ->
        let model' = { 
            model with 
                ActionSetting = {
                    model.ActionSetting with 
                        AccessoryId = uniqueId
                        CharacteristicType = None
                }
        }
        model |> sdDispatch (PiOut_SetSettings model'.ActionSetting)
        model', Cmd.none
    | SelectedCharacteristic characteristicType ->
        let model'= { 
            model with 
                ActionSetting = {
                    model.ActionSetting with 
                        CharacteristicType = characteristicType
                } 
        }
        model |> sdDispatch (PiOut_SetSettings model'.ActionSetting)
        model', Cmd.none
    | ToggleCharacteristic ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            match model.AuthInfo, model.ActionSetting.AccessoryId, model.ActionSetting.CharacteristicType with
            | Ok(authInfo), Some(selectedAccessoryId), Some(characteristicType) ->
                async {
                    let! accessory = Client.getAccessory model.ServerInfo.Host authInfo selectedAccessoryId
                    let currentValue = accessory.Value |> getIntValue characteristicType
                    let targetValue = 1 - currentValue
                    let! accessory' = Client.setAccessoryCharacteristic model.ServerInfo.Host authInfo selectedAccessoryId characteristicType targetValue
                    dispatch <| UpdateAccessory (filterBoolCharacteristics accessory'.Value)
                    model |> sdDispatch (PiOut_SendToPlugin accessory'.Value)
                } |> Async.StartImmediate
            | _ -> ()
        model, Cmd.ofSub delayedCmd
    | UpdateAccessory accessory ->
        let accessories' =
            model.Accessories
            |> Map.remove accessory.uniqueId
            |> Map.add accessory.uniqueId accessory
        { model with Accessories = accessories' }, Cmd.none

let view model dispatch =
    div [Class "sdpi-wrapper"] [
        match model.AuthInfo with
        | Error (error) ->
            div [Class "sdpi-item"; Type "field"] [
                div [Class "sdpi-item-label"] [str "Server"]
                input [
                    Class "sdpi-item-value"
                    Value model.ServerInfo.Host
                    Placeholder "e.g. http://192.168.0.1:8581"
                    Required true
                    Pattern "http:\/\/\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}:\\d{2,5}"
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
            if not <| System.String.IsNullOrEmpty(error) then
                div [Class "sdpi-item"; Type "button"] [
                    textarea [Type "textarea"; MaxLength 300] [str error]
                ]
        | Ok _ ->
            div [Class "sdpi-item"; Type "button"] [
                button [
                    Class "sdpi-item-value"; 
                    OnClick (fun _ -> dispatch <| Logout)
                ] [ str "Logout" ]
            ]
            div [Class "sdpi-item"] [
                div [Class "sdpi-item-label"] [str "Accessory"]
                select [
                    Class "sdpi-item-value select"
                    Value (model.ActionSetting.AccessoryId |> Option.defaultValue "DEFAULT")
                    OnChange (fun x -> 
                        let msg = if x.Value = "DEFAULT" then None else Some x.Value
                        dispatch <| SelectedAccessory msg)
                ] [
                    option [Value "DEFAULT"] []
                    for room in model.Layout do
                        optgroup [Label room.name] [
                            yield! room.services
                            |> Array.choose (fun x -> Map.tryFind x.uniqueId model.Accessories)
                            |> Array.sortBy (fun x -> x.serviceName)
                            |> Array.map (fun item -> 
                                option [Value item.uniqueId] [
                                    str <| sprintf "%s - %s" item.humanType item.serviceName
                                ]
                            )
                        ]
                ]
            ]
            match model.ActionSetting.AccessoryId with
            | Some(uniqueId) when model.Accessories.Count > 0 ->
                let accessory = model.Accessories |> Map.find uniqueId
                div [Class "sdpi-item"] [
                    div [Class "sdpi-item-label"] [str "Characteristic"]
                    select [
                        Class "sdpi-item-value select"
                        Value (model.ActionSetting.CharacteristicType |> Option.defaultValue "DEFAULT")
                        OnChange (fun x -> 
                            let msg = if x.Value = "DEFAULT" then None else Some x.Value
                            dispatch <| SelectedCharacteristic msg)
                    ] [
                        option [Value "DEFAULT"] []
                        let characteristics = accessory.serviceCharacteristics |> Array.sortBy (fun x -> x.``type``)
                        for x in characteristics do
                            option [Value x.``type``] [
                                str x.description
                            ]
                    ]
                ]
                match model.IsDevMode, model.ActionSetting.CharacteristicType with
                | true, Some _ ->
                    div [Class "sdpi-item"] [
                        button [
                            Class "sdpi-item-value"; 
                            OnClick (fun _ -> dispatch <| ToggleCharacteristic)
                        ] [
                            str "Test configuration (Apply)"
                        ]
                    ]
                | _ -> ()

                match model.ActionSetting.CharacteristicType with
                | Some characteristicType ->
                    let ai = accessory.accessoryInformation
                    let ch =
                        accessory.serviceCharacteristics
                        |> Array.find (fun x -> x.``type`` = characteristicType)

                    details [Class "message"] [
                        summary [] [str "More Info"]
                        h4 [] [str "Accessory Information"]
                        p [] [
                            str "Manufacturer: "; str ai.Manufacturer; br []
                            str "Model: "; str ai.Model]
                        h4 [] [str "Service Characteristics"]
                        p [] [
                            str "Service Type: "; str ch.serviceType; br []
                            str "Service Name: "; str ch.serviceName; br []
                            str "Description: "; str ch.description]
                    ]
                | None -> ()
            | _ -> ()
    ]