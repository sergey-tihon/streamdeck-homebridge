module StreamDeck.Homebridge.PiView

open System.Text.RegularExpressions
open Browser
open Elmish
open Feliz
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel
open StreamDeck.SDK.Css
open StreamDeck.SDK.Components

type PiModel = {
    IsDevMode: bool
    ReplyAgent: MailboxProcessor<PiOutEvent> option
    ServerInfo: Domain.GlobalSettings
    Client: Result<Client.HomebridgeClient, string>

    IsLoading: Result<bool, string>
    Accessories: Map<string, Client.AccessoryDetails>
    SwitchAccessories: Map<string, Client.AccessoryDetails>
    RangeAccessories: Map<string, Client.AccessoryDetails>
    Layout: Client.RoomLayout[]

    ActionType: string option
    ActionSetting: Domain.ActionSetting
}

[<RequireQualifiedAccess>]
type PiMsg =
    | PiConnected of startArgs: StartArgs * replyAgent: MailboxProcessor<PiOutEvent>
    | GlobalSettingsReceived of Domain.GlobalSettings
    | ActionSettingReceived of Domain.ActionSetting

    | UpdateServerInfo of Domain.GlobalSettings
    | Login of manual: bool
    | SetHomebridgeClient of Result<Client.HomebridgeClient, string>
    | Logout
    | GetData
    | SetData of Client.AccessoryDetails[] * Client.RoomLayout[]
    | ResetLoading of error: string
    | SelectActionType of actionType: string option
    | SelectAccessory of uniqueId: string option
    | SelectCharacteristic of characteristicType: string option
    | ChangeTargetValue of targetValue: float option
    | ToggleCharacteristic

let init isDevMode =
    fun () ->
        let state = {
            IsDevMode = isDevMode
            ReplyAgent = None
            ServerInfo = {
                Host = "http://192.168.68.65:8581"
                UserName = "admin"
                Password = "admin"
                UpdateInterval = 5
            }
            Client = Error null
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

let filterCharacteristics filter (accessory: Client.AccessoryDetails) = {
    accessory with
        serviceCharacteristics = accessory.serviceCharacteristics |> Array.filter filter
}

let hasValue(ch: Client.AccessoryServiceCharacteristic) =
    match ch.value with
    | Some x when not(isNull x) -> true
    | _ -> false

let filterBoolCharacteristics =
    filterCharacteristics(fun x ->
        x.canWrite
        && hasValue x // we can modify value
        && (x.format = "bool" // it is boolean
            || (x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1)) // or behave like boolean
    )

let filterRangeCharacteristics =
    filterCharacteristics(fun x ->
        if x.canWrite && hasValue x then
            match x.format, x.minValue, x.maxValue with
            | "uint8", Some a, Some b when b - a > 1 -> true
            | "float", Some a, Some b when b - a > 1 -> true
            | "int", Some a, Some b when b - a > 1 -> true
            | _ -> false
        else
            false)

let getCharacteristic characteristicType (accessory: Client.AccessoryDetails) =
    accessory.serviceCharacteristics
    |> Array.find(fun x -> x.``type`` = characteristicType)

let sdDispatch msg (model: PiModel) =
    match model.ReplyAgent with
    | Some(agent) -> agent.Post msg
    | None -> console.error("Message send before replyAgent assigned", msg)

let update (msg: PiMsg) (model: PiModel) =
    match msg with
    | PiMsg.PiConnected(startArgs, replyAgent) ->
        let model' = {
            model with
                ReplyAgent = Some replyAgent
                ActionType = startArgs.ActionInfo |> Option.map(fun x -> x.action)
        }

        let model' =
            startArgs.ActionInfo
            |> Option.map(fun x -> x.payload.settings)
            |> Option.bind(Domain.tryParse<Domain.ActionSetting>)
            |> function
                | Some(x) -> { model' with ActionSetting = x }
                | None -> model'

        model', Cmd.none
    | PiMsg.GlobalSettingsReceived settings ->
        let model' = {
            model with
                ServerInfo = settings
                Client = Error null
        }

        model', Cmd.ofMsg(PiMsg.Login false)
    | PiMsg.ActionSettingReceived settings ->
        let model' = { model with ActionSetting = settings }
        model', Cmd.none
    | PiMsg.UpdateServerInfo serverInfo ->
        let model' = {
            model with
                ServerInfo = serverInfo
                Client = Error null
        }

        model', Cmd.none
    | PiMsg.Login manual ->
        let delayedCmd(dispatch: PiMsg -> unit) : unit =
            async {
                let client = Client.HomebridgeClient(model.ServerInfo)
                let! result = client.TestAuth()

                match manual, result with
                | true, Ok _ -> model |> sdDispatch(PiOutEvent.SetGlobalSettings model.ServerInfo)
                | _ -> ()

                result
                |> Result.map(fun () -> client)
                |> PiMsg.SetHomebridgeClient
                |> dispatch

            }
            |> Async.StartImmediate

        { model with IsLoading = Ok true }, Cmd.ofEffect delayedCmd
    | PiMsg.SetHomebridgeClient client ->
        let cmd =
            match client with
            | Ok _ -> Cmd.ofMsg PiMsg.GetData
            | _ -> Cmd.none

        let model' = {
            model with
                Client = client
                IsLoading = Ok false
        }

        model', cmd
    | PiMsg.Logout -> { model with Client = Error null }, Cmd.none
    | PiMsg.GetData ->
        let delayedCmd(dispatch: PiMsg -> unit) : unit =
            async {
                match model.Client with
                | Ok client ->
                    let! layout = client.GetAccessoriesLayout()

                    let layout =
                        match layout with
                        | Ok layout -> layout
                        | Error err -> [||]

                    match! client.GetAccessories() with
                    | Ok(accessories) ->
                        let devicesInLayout =
                            layout
                            |> Array.collect(fun room -> room.services |> Array.map(fun x -> x.uniqueId))
                            |> Set.ofArray

                        let missingDevices =
                            accessories
                            |> Array.filter(fun x -> devicesInLayout |> Set.contains x.uniqueId |> not)

                        let layout' =
                            if missingDevices.Length = 0 then
                                layout
                            else
                                Array.append layout [|
                                    {
                                        ``name`` = "Others"
                                        services =
                                            missingDevices
                                            |> Array.map(fun x -> {
                                                uniqueId = x.uniqueId
                                                aid = x.aid
                                                iid = x.iid
                                                uuid = x.uuid
                                                customName = Some x.serviceName
                                            })
                                    }
                                |]

                        dispatch <| PiMsg.SetData(accessories, layout')
                    | Error e -> dispatch <| PiMsg.ResetLoading $"Cannot get list of accessories. {e}"
                | Error e -> dispatch <| PiMsg.ResetLoading $"User is not authenticated. {e}"
            }
            |> Async.StartImmediate

        { model with IsLoading = Ok true }, Cmd.ofEffect delayedCmd
    | PiMsg.SetData(accessories, layout) ->
        let toMap(accessories: Client.AccessoryDetails[]) =
            accessories |> Array.map(fun x -> x.uniqueId, x) |> Map.ofArray

        let state = {
            model with
                Accessories = accessories |> toMap
                SwitchAccessories = accessories |> Array.map filterBoolCharacteristics |> toMap
                RangeAccessories = accessories |> Array.map filterRangeCharacteristics |> toMap
                Layout = layout
                IsLoading = Ok false
        }

        state, Cmd.none
    | PiMsg.ResetLoading error -> { model with IsLoading = Error error }, Cmd.none
    | PiMsg.SelectActionType actionType -> { model with ActionType = actionType }, Cmd.none
    | PiMsg.SelectAccessory uniqueId ->
        let model' = {
            model with
                ActionSetting = {
                    model.ActionSetting with
                        AccessoryId = uniqueId
                        CharacteristicType = None
                        TargetValue = None
                }
        }

        model |> sdDispatch(PiOutEvent.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.SelectCharacteristic characteristicType ->
        let targetValue =
            match characteristicType, model.ActionSetting.AccessoryId with
            | Some(characteristicType), Some(accessoryId) ->
                let ch =
                    model.Accessories
                    |> Map.find accessoryId
                    |> getCharacteristic characteristicType

                Some(ch.value.Value :?> float)
            | _ -> None

        let model' = {
            model with
                ActionSetting = {
                    model.ActionSetting with
                        CharacteristicType = characteristicType
                        TargetValue = targetValue
                }
        }

        model |> sdDispatch(PiOutEvent.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.ChangeTargetValue targetValue ->
        let model' = {
            model with
                ActionSetting = {
                    model.ActionSetting with
                        TargetValue = targetValue
                }
        }

        model |> sdDispatch(PiOutEvent.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.ToggleCharacteristic ->
        let delayedCmd(dispatch: PiMsg -> unit) : unit =
            match model.Client, model.ActionSetting.AccessoryId, model.ActionSetting.CharacteristicType with
            | Ok(client), Some(selectedAccessoryId), Some(characteristicType) ->
                match model.ActionType with
                | Some(Domain.ActionName.Switch) ->
                    async {
                        match! client.GetAccessory selectedAccessoryId with
                        | Ok accessory ->
                            let! accessory' =
                                let ch = accessory |> getCharacteristic characteristicType
                                let currentValue = ch.value.Value :?> int
                                let targetValue = 1 - currentValue
                                client.SetAccessoryCharacteristic selectedAccessoryId characteristicType targetValue

                            match accessory' with
                            | Ok accessory' -> model |> sdDispatch(PiOutEvent.SendToPlugin accessory')
                            | Error e -> console.error(e)
                        | Error e -> console.error(e)
                    }
                    |> Async.StartImmediate
                | Some(Domain.ActionName.Set) ->
                    async {
                        let! accessory' =
                            let targetValue = model.ActionSetting.TargetValue.Value
                            client.SetAccessoryCharacteristic selectedAccessoryId characteristicType targetValue

                        match accessory' with
                        | Ok accessory' -> model |> sdDispatch(PiOutEvent.SendToPlugin accessory')
                        | Error e -> console.error(e)
                    }
                    |> Async.StartImmediate
                | _ -> console.error("Unexpected action ", model.ActionType)
            | _ -> ()

        model, Cmd.ofEffect delayedCmd

let render (model: PiModel) (dispatch: PiMsg -> unit) =

    let accessorySelector(accessories: Map<string, Client.AccessoryDetails>) = [
        match model.ActionSetting.AccessoryId with
        | Some(uniqueId) when not <| accessories.ContainsKey(uniqueId) ->
            Pi.message
                "caution"
                "red"
                "Configured device is no longer available on Homebridge. Please update Homebridge or Reset button config."

            Pi.button "Reset button config" (fun _ -> dispatch <| PiMsg.SelectAccessory None)
        | _ ->
            Pi.select "Accessory" [
                prop.value(model.ActionSetting.AccessoryId |> Option.defaultValue "DEFAULT")
                prop.children [
                    Html.option [ prop.value "DEFAULT" ]
                    for room in model.Layout do
                        Html.optgroup [
                            prop.custom("label", room.name)
                            //prop.label room.name
                            prop.children(
                                room.services
                                |> Array.toList
                                |> List.choose(fun itemInfo ->
                                    Map.tryFind itemInfo.uniqueId accessories
                                    |> Option.map(fun accessoryDetails ->
                                        let name =
                                            itemInfo.customName
                                            |> Option.defaultValue accessoryDetails.serviceName

                                        name, accessoryDetails))
                                |> List.sortBy fst
                                |> List.map(fun (name, accessoryDetails) ->
                                    Html.option [
                                        prop.value accessoryDetails.uniqueId
                                        prop.disabled(accessoryDetails.serviceCharacteristics.Length = 0)
                                        prop.text name
                                    ])
                            )
                        ]
                ]
                prop.onChange(fun (value: string) ->
                    //let value = x.Value
                    let msg = if value = "DEFAULT" then None else Some value
                    dispatch <| PiMsg.SelectAccessory msg)
            ]
    ]

    let characteristicSelector(accessory: Client.AccessoryDetails) =
        Pi.select "Characteristic" [
            prop.value(
                model.ActionSetting.CharacteristicType
                |> Option.defaultValue "DEFAULT"
            )
            prop.children [
                Html.option [ prop.value "DEFAULT" ]
                let characteristics =
                    accessory.serviceCharacteristics |> Array.sortBy(fun x -> x.``type``)

                for x in characteristics do
                    Html.option [ prop.value x.``type``; prop.text x.description ]
            ]
            prop.onChange(fun (value: string) ->
                let msg = if value = "DEFAULT" then None else Some value
                dispatch <| PiMsg.SelectCharacteristic msg)
        ]

    let successConfirmation = Pi.message "" "green" "Button successfully configured"

    Feliz.Html.div [
        prop.className SdPi.Wrapper
        prop.children [
            match model.IsLoading with
            | Ok true -> Pi.message "info" "orange" "Waiting for Homebridge API response ..."
            | _ ->
                match model.IsLoading with
                | Error error -> Pi.message "caution" "red" error
                | _ -> ()

                match model.Client with
                | Error(error) ->
                    if not <| System.String.IsNullOrEmpty(error) then
                        Pi.message "caution" "red" error

                    Pi.input "Server" [
                        prop.value model.ServerInfo.Host
                        prop.placeholder "e.g. http://192.168.68.65:8581"
                        prop.required true
                        prop.pattern(Regex "^(.*:)//([A-Za-z0-9\-\.]+)(:[0-9]+)?$")
                        prop.onChange(fun (value: string) ->
                            dispatch
                            <| PiMsg.UpdateServerInfo {
                                model.ServerInfo with
                                    Host =
                                        if value.Length > 10 then
                                            value.TrimEnd([| '/' |])
                                        else // we should not trim when user type only "http://"
                                            value
                            })
                    ]

                    Pi.input "UserName" [
                        prop.value model.ServerInfo.UserName
                        prop.required true
                        prop.onChange(fun value ->
                            dispatch
                            <| PiMsg.UpdateServerInfo {
                                model.ServerInfo with
                                    UserName = value
                            })
                    ]

                    Pi.input "Password" [
                        prop.type' "password"
                        prop.value model.ServerInfo.Password
                        prop.required true
                        prop.onChange(fun value ->
                            let settings = {
                                model.ServerInfo with
                                    Password = value
                            }

                            dispatch <| PiMsg.UpdateServerInfo settings)
                    ]

                    Pi.select "Update" [
                        prop.value model.ServerInfo.UpdateInterval
                        prop.children [
                            Html.option [ prop.value "0"; prop.text "Never" ]
                            Html.option [ prop.value "1"; prop.text "Every second" ]
                            Html.option [ prop.value "2"; prop.text "Every 2 seconds" ]
                            Html.option [ prop.value "5"; prop.text "Every 5 seconds" ]
                            Html.option [ prop.value "10"; prop.text "Every 10 seconds" ]
                            Html.option [ prop.value "60"; prop.text "Every minute" ]
                        ]
                        prop.onChange(fun (value: string) ->
                            {
                                model.ServerInfo with
                                    UpdateInterval = int(value)
                            }
                            |> PiMsg.UpdateServerInfo
                            |> dispatch)
                    ]

                    Pi.button "Login" (fun _ -> dispatch <| PiMsg.Login true)
                | Ok _ ->
                    match model.ActionType with
                    | None ->
                        Pi.select "Action Type" [
                            prop.value(model.ActionType |> Option.defaultValue "DEFAULT")
                            prop.children [
                                Html.option [ prop.value "DEFAULT" ]
                                Html.option [ prop.value Domain.ActionName.ConfigUi; prop.text "Config UI" ]
                                Html.option [ prop.value Domain.ActionName.Switch; prop.text "Switch" ]
                                Html.option [ prop.value Domain.ActionName.Set; prop.text "Set state" ]
                            ]
                            prop.onChange(fun value ->
                                let msg = if value = "DEFAULT" then None else Some value
                                dispatch <| PiMsg.SelectActionType msg)
                        ]
                    | Some(Domain.ActionName.ConfigUi) -> successConfirmation
                    | Some(Domain.ActionName.Switch) ->
                        yield! accessorySelector model.SwitchAccessories

                        match model.ActionSetting.AccessoryId with
                        | Some(uniqueId) when model.SwitchAccessories.ContainsKey uniqueId ->
                            let accessory = model.SwitchAccessories |> Map.find uniqueId
                            characteristicSelector accessory

                            match model.ActionSetting.CharacteristicType with
                            | Some characteristicType ->
                                if model.IsDevMode then
                                    Pi.button "Emit Switch action" (fun _ -> dispatch <| PiMsg.ToggleCharacteristic)

                                successConfirmation
                            //characteristicDetails characteristicType accessory
                            | None -> ()
                        | _ -> ()
                    | Some(Domain.ActionName.Set) ->
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
                                    Pi.range $"Target value ({targetValue})" [
                                        Html.span [
                                            prop.className "clickable"
                                            prop.value minValue
                                            prop.text $"{minValue}"
                                        ]
                                        Html.input [
                                            prop.type' "range"
                                            prop.min minValue
                                            prop.max maxValue
                                            prop.step minStep
                                            prop.value targetValue
                                            prop.onChange(fun (x: float) ->
                                                let payload = Some(x)
                                                dispatch <| PiMsg.ChangeTargetValue payload)
                                        ]
                                        Html.span [
                                            prop.className "clickable"
                                            prop.value maxValue
                                            prop.text $"{maxValue}"
                                        ]
                                    ]

                                    if model.IsDevMode then
                                        Pi.button "Emit Set action" (fun _ -> dispatch <| PiMsg.ToggleCharacteristic)

                                    successConfirmation
                                | _ -> ()
                            | _ -> ()
                        | _ -> ()
                    | Some ty -> Html.p [ prop.text $"Unsupported action type {ty}" ]

                    Pi.button "Logout" (fun _ -> dispatch <| PiMsg.Logout)
        ]
    ]
