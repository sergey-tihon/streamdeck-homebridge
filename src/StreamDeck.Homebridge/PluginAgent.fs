module StreamDeck.Homebridge.PluginAgent

open Browser.Dom
open StreamDeck.SDK.PluginModel

let createPluginAgent() :MailboxProcessor<PluginIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec idle() = async {
            let! msg = inbox.Receive()
            match msg with
            | PluginIn_Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PluginOut_GetGlobalSettings
                return! loop startArgs replyAgent None
            | _ -> 
                console.warn($"Idle plugin agent received unexpected message %A{msg}", msg)
                return! idle()
        }
        and loop startArgs replyAgent globalSetting = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)
            match msg with
            | PluginIn_DidReceiveGlobalSettings settings ->
                let settings =  Domain.tryParse<Domain.GlobalSettings>(settings)
                return! loop startArgs replyAgent settings
            | PluginIn_KeyUp(event, payload) ->
                let onError (message:string) = 
                    console.warn(message)
                    GTag.logException message
                    replyAgent.Post <| PluginOut_LogMessage message
                    replyAgent.Post <| PluginOut_ShowAlert event.context

                match event.action with
                | Domain.CONFIG_ACTION_NAME ->
                    match globalSetting with
                    | Some(serverInfo) -> replyAgent.Post <| PluginOut_OpenUrl serverInfo.Host
                    | _ ->  onError "Global config is not provided"
                | Domain.SWITCH_ACTION_NAME ->
                    let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)
                    match globalSetting, actionSettings with
                    | Some(serverInfo), Some({ AccessoryId = Some(accessoryId); CharacteristicType = Some(characteristicType)}) ->
                        match! Client.authenticate serverInfo with
                        | Ok authInfo ->
                            let! accessory = Client.getAccessory serverInfo.Host authInfo accessoryId
                            match accessory with
                            | Ok accessory ->
                                let ch = accessory |> PiView.getCharacteristic characteristicType
                                let currentValue = ch.value.Value :?> int
                                let targetValue = 1 - currentValue
                                let! accessory' = Client.setAccessoryCharacteristic serverInfo.Host authInfo accessoryId characteristicType targetValue
                                match accessory' with
                                | Ok accessory' ->
                                    let ch' = accessory' |> PiView.getCharacteristic characteristicType
                                    let currentValue' = ch'.value.Value :?> int
                                    if currentValue = currentValue' 
                                    then replyAgent.Post <| PluginOut_ShowAlert event.context
                                    else replyAgent.Post <| PluginOut_SetState (event.context, currentValue') 
                                | Error e -> onError e
                            | Error e -> onError $"Cannot find accessory by id '{accessoryId}'. {e}"
                        | Error e -> onError $"Authentication issue: {e}"
                    | _ ->  onError "Action is not properly configured"
                | Domain.SET_ACTION_NAME ->
                    let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)
                    match globalSetting, actionSettings with
                    | Some(serverInfo), Some({ AccessoryId = Some(accessoryId); CharacteristicType = Some(characteristicType); TargetValue = Some(targetValue)}) ->
                        match! Client.authenticate serverInfo with
                        | Ok authInfo ->
                            let! accessory = Client.setAccessoryCharacteristic serverInfo.Host authInfo accessoryId characteristicType targetValue
                            match accessory with
                            | Ok accessory ->
                                let ch = accessory |> PiView.getCharacteristic characteristicType
                                let currentValue = ch.value.Value :?> float
                                if abs(targetValue - currentValue) > 1e-8
                                then replyAgent.Post <| PluginOut_ShowAlert event.context
                                else replyAgent.Post <| PluginOut_ShowOk event.context
                            | Error e -> onError e
                        | Error e -> onError $"Authentication issue: {e}"
                    | _ ->  onError "Action is not properly configured"
                | _ -> onError $"Action {event.action} is not yet supported"

                return! loop startArgs replyAgent globalSetting
            | _ ->
                return! loop startArgs replyAgent globalSetting
        }
        idle()
    )
