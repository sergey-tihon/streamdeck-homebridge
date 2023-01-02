module StreamDeck.Homebridge.PluginAgent

open Browser.Dom
open StreamDeck.SDK
open StreamDeck.SDK.PluginModel
open System

type PluginInnerState = {
    replyAgent: MailboxProcessor<PluginOutEvent>
    client: Client.HomebridgeClient option
    lastCharacteristicUpdate: DateTime
    characteristics: Map<string * string, Client.AccessoryServiceCharacteristic>
    visibleActions: Map<string, Domain.ActionSetting * int option>
    updateInterval: int
    timerId: float option
}

let processKeyUp (state: PluginInnerState) (event: Dto.Event) (payload: Dto.ActionPayload) =
    let onError(message: string) =
        console.error(message)
        state.replyAgent.Post <| PluginOutEvent.LogMessage message
        state.replyAgent.Post <| PluginOutEvent.ShowAlert event.context

    async {
        match event.action with
        | Domain.ActionName.ConfigUi ->
            match state.client with
            | Some(client) -> state.replyAgent.Post <| PluginOutEvent.OpenUrl client.Host
            | _ -> onError "Global config is not provided"
        | Domain.ActionName.Switch ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match state.client, actionSettings with
            | Some(client),
              Some({
                       AccessoryId = Some accessoryId
                       CharacteristicType = Some characteristicType
                   }) ->
                match state.characteristics |> Map.tryFind(accessoryId, characteristicType) with
                | Some(ch) ->
                    let currentValue = ch.value.Value :?> int
                    let targetValue = 1 - currentValue

                    match! client.SetAccessoryCharacteristic accessoryId characteristicType targetValue with
                    | Ok accessory ->
                        let ch' = accessory |> PiView.getCharacteristic characteristicType
                        let updatedValue = ch'.value.Value :?> int

                        if targetValue = updatedValue then
                            state.replyAgent.Post <| PluginOutEvent.ShowOk event.context
                    | Error e -> onError e
                | _ -> onError $"Cannot find characteristic by id '{accessoryId}, {characteristicType}'."
            | _ -> onError "Action is not properly configured"
        | Domain.ActionName.Set ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match state.client, actionSettings with
            | Some(client),
              Some({
                       AccessoryId = Some accessoryId
                       CharacteristicType = Some characteristicType
                       TargetValue = Some targetValue
                   }) ->
                match! client.SetAccessoryCharacteristic accessoryId characteristicType targetValue with
                | Ok accessory ->
                    let ch = accessory |> PiView.getCharacteristic characteristicType
                    let currentValue = ch.value.Value :?> float

                    if abs(targetValue - currentValue) < 1e-8 then
                        state.replyAgent.Post <| PluginOutEvent.ShowOk event.context
                | Error e -> onError e
            | _ -> onError "Action is not properly configured"
        | _ -> onError $"Action {event.action} is not yet supported"
    }

let updateAccessories(state: PluginInnerState) = async {
    let now = DateTime.Now

    if now - state.lastCharacteristicUpdate < TimeSpan.FromSeconds 1.0 then
        return state
    else
        let! accessories =
            state.client
            |> Option.map(fun client -> client.GetAccessories())
            |> Option.defaultValue(async { return Error("Homedbridge client is not set yet") })

        let characteristics =
            match accessories with
            | Error _ -> state.characteristics
            | Ok(accessories) ->
                accessories
                |> Array.collect(fun accessory ->
                    accessory.serviceCharacteristics
                    |> Array.map(fun characteristic ->
                        let key = accessory.uniqueId, characteristic.``type``
                        key, characteristic))
                |> Map.ofArray

        return
            { state with
                characteristics = characteristics
                lastCharacteristicUpdate = now
            }
}

let updateActions(state: PluginInnerState) = async {
    let! state = updateAccessories state

    let visibleActions =
        state.visibleActions
        |> Map.map(fun context value ->
            match value with
            | {
                  AccessoryId = Some accessoryId
                  CharacteristicType = Some characteristicType
              },
              Some actionState ->
                match state.characteristics |> Map.tryFind(accessoryId, characteristicType) with
                | Some(ch) when ch.value.IsSome ->
                    let chValue = ch.value.Value :?> int

                    if actionState <> chValue then
                        state.replyAgent.Post <| PluginOutEvent.SetState(context, chValue)
                        (fst value, Some(chValue))
                    else
                        value
                | _ -> value
            | _ -> value)

    return
        { state with
            visibleActions = visibleActions
        }
}


let createPluginAgent() : MailboxProcessor<PluginInEvent> =
    let mutable agent: MailboxProcessor<PluginInEvent> option = None

    let updateTimer(state: PluginInnerState) =
        if state.timerId.IsSome then
            window.clearTimeout(state.timerId.Value)

        let timerId =
            if state.updateInterval = 0 then
                None
            else
                window.setInterval(
                    (fun _ -> agent.Value.Post(PluginInEvent.SystemDidWakeUp)),
                    1000 * state.updateInterval,
                    [||]
                )
                |> Some

        { state with timerId = timerId }


    agent <-
        MailboxProcessor.Start(fun inbox ->
            let rec idle() = async {
                let! msg = inbox.Receive()

                match msg with
                | PluginInEvent.Connected(_, replyAgent) ->
                    replyAgent.Post <| PluginOutEvent.GetGlobalSettings

                    let state = {
                        replyAgent = replyAgent
                        client = None
                        lastCharacteristicUpdate = DateTime.MinValue
                        characteristics = Map.empty
                        visibleActions = Map.empty
                        updateInterval = 0
                        timerId = None
                    }

                    return! loop state
                | _ ->
                    console.warn($"Idle plugin agent received unexpected message %A{msg}", msg)
                    return! idle()
            }

            and loop state = async {
                let! msg = inbox.Receive()
                console.log($"Plugin message is: %A{msg}", msg)

                match msg with
                | PluginInEvent.DidReceiveGlobalSettings settings ->
                    let state =
                        match Domain.tryParse<Domain.GlobalSettings>(settings) with
                        | Some(settings) ->
                            { state with
                                client = Some(Client.HomebridgeClient settings)
                                updateInterval = settings.UpdateInterval
                            }
                            |> updateTimer
                        | _ -> state

                    return! loop state
                | PluginInEvent.KeyUp(event, payload) ->
                    let! state = updateActions state
                    do! processKeyUp state event payload
                    return! loop state
                | PluginInEvent.SystemDidWakeUp ->
                    // Fake action triggered by timer to update buttons state
                    let! state = updateActions state
                    return! loop state
                | PluginInEvent.WillAppear(event, payload) ->
                    let! state = updateActions state

                    let state =
                        { state with
                            visibleActions =
                                match Domain.tryParse<Domain.ActionSetting>(payload.settings) with
                                | Some(actionSetting) when event.action = Domain.ActionName.Switch ->
                                    state.visibleActions
                                    |> Map.add event.context (actionSetting, payload.state)
                                | _ -> state.visibleActions
                        }
                        |> updateTimer

                    return! loop state
                | PluginInEvent.WillDisappear(event, _) ->
                    let actions = state.visibleActions |> Map.remove event.context

                    let state =
                        { state with
                            visibleActions = actions
                            timerId =
                                match state.timerId with
                                | Some timerId when actions.IsEmpty ->
                                    window.clearInterval timerId
                                    None
                                | _ -> state.timerId
                        }

                    return! loop state
                | _ -> return! loop state
            }

            idle())
        |> Some

    agent.Value
