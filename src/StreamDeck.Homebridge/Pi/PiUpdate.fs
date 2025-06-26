module StreamDeck.Homebridge.PiUpdate

open Browser
open Elmish
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel
open StreamDeck.Homebridge
open StreamDeck.Homebridge.PiModel

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
            || x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1) // or behave like boolean
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
    | Some agent -> agent.Post msg
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
            |> Option.bind Domain.tryParse<Domain.ActionSetting>
            |> function
                | Some x -> { model' with ActionSetting = x }
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
                let client = Client.HomebridgeClient model.ServerInfo
                let! result = client.TestAuth()

                match manual, result with
                | true, Ok _ -> model |> sdDispatch(PiCommand.SetGlobalSettings model.ServerInfo)
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
                        | Error _ -> [||]

                    match! client.GetAccessories() with
                    | Ok accessories ->
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

        model |> sdDispatch(PiCommand.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.SelectCharacteristic characteristicType ->
        let targetValue =
            match characteristicType, model.ActionSetting.AccessoryId with
            | Some characteristicType, Some accessoryId ->
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

        model |> sdDispatch(PiCommand.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.ChangeTargetValue targetValue ->
        let model' = {
            model with
                ActionSetting = {
                    model.ActionSetting with
                        TargetValue = targetValue
                }
        }

        model |> sdDispatch(PiCommand.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.ChangeSpeed speed ->
        let model' = {
            model with
                ActionSetting = {
                    model.ActionSetting with
                        Speed = speed
                }
        }

        model |> sdDispatch(PiCommand.SetSettings model'.ActionSetting)
        model', Cmd.none
    | PiMsg.EmitEvent payload ->
        let delayedCmd(_: PiMsg -> unit) : unit =
            match model.Client, model.ActionSetting.AccessoryId, model.ActionSetting.CharacteristicType with
            | Ok client, Some selectedAccessoryId, Some characteristicType ->
                match model.ActionType with
                | Some Domain.ActionName.Switch ->
                    async {
                        match! client.GetAccessory selectedAccessoryId with
                        | Ok accessory ->
                            let! accessory' =
                                let ch = accessory |> getCharacteristic characteristicType
                                let currentValue = ch.value.Value :?> int
                                let targetValue = 1 - currentValue
                                client.SetAccessoryCharacteristic selectedAccessoryId characteristicType targetValue

                            match accessory' with
                            | Ok accessory' -> model |> sdDispatch(PiCommand.SendToPlugin accessory')
                            | Error e -> console.error e
                        | Error e -> console.error e
                    }
                    |> Async.StartImmediate
                | Some Domain.ActionName.Set ->
                    async {
                        let! accessory' =
                            let targetValue = model.ActionSetting.TargetValue.Value
                            client.SetAccessoryCharacteristic selectedAccessoryId characteristicType targetValue

                        match accessory' with
                        | Ok accessory' -> model |> sdDispatch(PiCommand.SendToPlugin accessory')
                        | Error e -> console.error e
                    }
                    |> Async.StartImmediate
                | Some Domain.ActionName.Adjust ->
                    let delta =
                        match payload with
                        | Some x -> x
                        | _ ->
                            console.error "adjust action without payload"
                            0

                    async {
                        match! client.GetAccessory selectedAccessoryId with
                        | Ok accessory ->
                            let! accessory' =
                                let ch = accessory |> getCharacteristic characteristicType
                                let currentValue = ch.value.Value :?> int
                                let step = ch.minStep.Value
                                let speed = model.ActionSetting.Speed |> Option.defaultValue 1

                                let targetValue =
                                    currentValue + delta * step * speed
                                    |> min ch.maxValue.Value
                                    |> max ch.minValue.Value

                                client.SetAccessoryCharacteristic selectedAccessoryId characteristicType targetValue

                            match accessory' with
                            | Ok accessory' -> model |> sdDispatch(PiCommand.SendToPlugin accessory')
                            | Error e -> console.error e
                        | Error e -> console.error e
                    }
                    |> Async.StartImmediate
                | _ -> console.error("Unexpected action ", model.ActionType)
            | _ -> ()

        model, Cmd.ofEffect delayedCmd
