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
                    replyAgent.Post <| PluginOut_LogMessage message
                    replyAgent.Post <| PluginOut_ShowAlert event.context

                match event with
                | Domain.ConfigAction ->
                    match globalSetting with
                    | Some(serverInfo) -> replyAgent.Post <| PluginOut_OpenUrl serverInfo.Host
                    | _ ->  onError "Global config is not provided"
                | Domain.SwitchAction ->
                    let actionSettings = Domain.tryParse<Domain.ActionSetting>(payload.settings)
                    match globalSetting, actionSettings with
                    | Some(serverInfo), Some({ AccessoryId = Some(accessoryId); CharacteristicType = Some(characteristicType)}) ->
                        match! Client.authenticate serverInfo with
                        | Ok authInfo ->
                            let! accessory = Client.getAccessory serverInfo.Host authInfo accessoryId
                            match accessory with
                            | Some(accessory) ->
                                let currentValue = accessory |> PiView.getIntValue characteristicType
                                let targetValue = 1 - currentValue
                                let! accessory' = Client.setAccessoryCharacteristic serverInfo.Host authInfo accessoryId characteristicType targetValue
                                match accessory' with
                                | Some(accessory') ->
                                    let currentValue' = accessory' |> PiView.getIntValue characteristicType
                                    if currentValue = currentValue' 
                                    then replyAgent.Post <| PluginOut_ShowAlert event.context
                                    else replyAgent.Post <| PluginOut_SetState (event.context, currentValue') 
                                | None -> onError $"Cannot toggle characteristic '{characteristicType}' of accessory '{accessoryId}'"
                            | None -> onError $"Cannot find accessory by id '{accessoryId}'"
                        | Error e -> onError $"Authentication issue: {e}"
                    | _ ->  onError "Action is not properly configured"
                | _ -> onError $"Action {event.action} is not yet supported"

                return! loop startArgs replyAgent globalSetting
            | _ ->
                return! loop startArgs replyAgent globalSetting
        }
        idle()
    )

