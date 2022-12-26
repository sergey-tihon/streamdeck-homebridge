module StreamDeck.Homebridge.PluginAgent

open Browser.Dom
open StreamDeck.SDK
open StreamDeck.SDK.PluginModel

let processKeyUp
    (replyAgent: MailboxProcessor<PluginOutEvent>)
    (globalSetting: Domain.GlobalSettings option)
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
            match globalSetting with
            | Some(serverInfo) -> replyAgent.Post <| PluginOutEvent.OpenUrl serverInfo.Host
            | _ -> onError "Global config is not provided"
        | Domain.ActionName.Switch ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match globalSetting, actionSettings with
            | Some(serverInfo),
              Some({
                       AccessoryId = Some(accessoryId)
                       CharacteristicType = Some(characteristicType)
                   }) ->
                match! Client.authenticate serverInfo with
                | Ok authInfo ->
                    let! accessory = Client.getAccessory serverInfo.Host authInfo accessoryId

                    match accessory with
                    | Ok accessory ->
                        let ch = accessory |> PiView.getCharacteristic characteristicType
                        let currentValue = ch.value.Value :?> int
                        let targetValue = 1 - currentValue

                        let! accessory' =
                            Client.setAccessoryCharacteristic
                                serverInfo.Host
                                authInfo
                                accessoryId
                                characteristicType
                                targetValue

                        match accessory' with
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
                | Error e -> onError $"Authentication issue: {e}"
            | _ -> onError "Action is not properly configured"
        | Domain.ActionName.Set ->
            let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)

            match globalSetting, actionSettings with
            | Some(serverInfo),
              Some({
                       AccessoryId = Some(accessoryId)
                       CharacteristicType = Some(characteristicType)
                       TargetValue = Some(targetValue)
                   }) ->
                match! Client.authenticate serverInfo with
                | Ok authInfo ->
                    let! accessory =
                        Client.setAccessoryCharacteristic
                            serverInfo.Host
                            authInfo
                            accessoryId
                            characteristicType
                            targetValue

                    match accessory with
                    | Ok accessory ->
                        let ch = accessory |> PiView.getCharacteristic characteristicType
                        let currentValue = ch.value.Value :?> float

                        if abs(targetValue - currentValue) > 1e-8 then
                            replyAgent.Post <| PluginOutEvent.ShowAlert event.context
                        else
                            replyAgent.Post <| PluginOutEvent.ShowOk event.context
                    | Error e -> onError e
                | Error e -> onError $"Authentication issue: {e}"
            | _ -> onError "Action is not properly configured"
        | _ -> onError $"Action {event.action} is not yet supported"
    }

let createPluginAgent() : MailboxProcessor<PluginInEvent> =
    MailboxProcessor.Start(fun inbox ->
        let rec idle() = async {
            let! msg = inbox.Receive()

            match msg with
            | PluginInEvent.Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PluginOutEvent.GetGlobalSettings
                return! loop startArgs replyAgent None
            | _ ->
                console.warn($"Idle plugin agent received unexpected message %A{msg}", msg)
                return! idle()
        }

        and loop startArgs replyAgent globalSetting = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)

            match msg with
            | PluginInEvent.DidReceiveGlobalSettings settings ->
                let settings = Domain.tryParse<Domain.GlobalSettings>(settings)
                return! loop startArgs replyAgent settings
            | PluginInEvent.KeyUp(event, payload) ->
                do! processKeyUp replyAgent globalSetting event payload
                return! loop startArgs replyAgent globalSetting
            | _ -> return! loop startArgs replyAgent globalSetting
        }

        idle())
