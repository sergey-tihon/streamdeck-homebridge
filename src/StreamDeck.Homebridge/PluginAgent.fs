module StreamDeck.Homebridge.PluginAgent

open Browser.Dom
open StreamDeck.SDK
open StreamDeck.SDK.PluginModel

let processKeyUp
    (replyAgent: MailboxProcessor<PluginOutEvent>)
    (client: Client.HomebridgeClient option)
    (event: Dto.Event)
    (payload: Dto.ActionPayload)
    =

    let onError(message: string) =
        console.warn(message)
        replyAgent.Post <| PluginOutEvent.LogMessage message
        replyAgent.Post <| PluginOutEvent.ShowAlert event.context

    async {
        match event.action with
        | Domain.ActionName.ConfigUi ->
            match client with
            | Some(client) -> replyAgent.Post <| PluginOutEvent.OpenUrl client.Host
            | _ -> onError "Global config is not provided"
        | Domain.ActionName.Switch ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match client, actionSettings with
            | Some(client),
              Some({
                       AccessoryId = Some(accessoryId)
                       CharacteristicType = Some(characteristicType)
                   }) ->
                match! client.GetAccessory accessoryId with
                | Ok accessory ->
                    let ch = accessory |> PiView.getCharacteristic characteristicType
                    let currentValue = ch.value.Value :?> int
                    let targetValue = 1 - currentValue

                    match! client.SetAccessoryCharacteristic accessoryId characteristicType targetValue with
                    | Ok accessory' ->
                        let ch' = accessory' |> PiView.getCharacteristic characteristicType
                        let currentValue' = ch'.value.Value :?> int

                        if currentValue = currentValue' then
                            replyAgent.Post <| PluginOutEvent.ShowAlert event.context
                        else
                            replyAgent.Post
                            <| PluginOutEvent.SetState(event.context, currentValue')
                    | Error e -> onError e
                | Error e -> onError $"Cannot find accessory by id '{accessoryId}'. {e}"
            | _ -> onError "Action is not properly configured"
        | Domain.ActionName.Set ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match client, actionSettings with
            | Some(client),
              Some({
                       AccessoryId = Some(accessoryId)
                       CharacteristicType = Some(characteristicType)
                       TargetValue = Some(targetValue)
                   }) ->
                match! client.SetAccessoryCharacteristic accessoryId characteristicType targetValue with
                | Ok accessory ->
                    let ch = accessory |> PiView.getCharacteristic characteristicType
                    let currentValue = ch.value.Value :?> float

                    if abs(targetValue - currentValue) > 1e-8 then
                        replyAgent.Post <| PluginOutEvent.ShowAlert event.context
                    else
                        replyAgent.Post <| PluginOutEvent.ShowOk event.context
                | Error e -> onError e
            | _ -> onError "Action is not properly configured"
        | _ -> onError $"Action {event.action} is not yet supported"
    }

let createPluginAgent() : MailboxProcessor<PluginInEvent> =
    MailboxProcessor.Start(fun inbox ->
        let rec idle() = async {
            let! msg = inbox.Receive()

            match msg with
            | PluginInEvent.Connected(_, replyAgent) ->
                replyAgent.Post <| PluginOutEvent.GetGlobalSettings
                return! loop replyAgent None
            | _ ->
                console.warn($"Idle plugin agent received unexpected message %A{msg}", msg)
                return! idle()
        }

        and loop replyAgent client = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)

            match msg with
            | PluginInEvent.DidReceiveGlobalSettings settings ->
                let client =
                    Domain.tryParse<Domain.GlobalSettings>(settings)
                    |> Option.map(Client.HomebridgeClient)

                return! loop replyAgent client
            | PluginInEvent.KeyUp(event, payload) ->
                do! processKeyUp replyAgent client event payload
                return! loop replyAgent client
            | _ -> return! loop replyAgent client
        }

        idle())
