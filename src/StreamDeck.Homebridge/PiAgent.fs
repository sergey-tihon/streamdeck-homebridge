module StreamDeck.Homebridge.PiAgent

open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel

let createPiAgent (dispatch: PiView.PiMsg -> unit) :MailboxProcessor<PiIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec loop() = async{
            let! msg = inbox.Receive()
            console.log($"PI message is: %A{msg}", msg)

            match msg with
            | PiIn_Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PiOut_GetGlobalSettings
                dispatch <| PiView.PiConnected(startArgs, replyAgent)
            | PiIn_DidReceiveSettings(event, payload) ->
                Domain.tryParse<Domain.ActionSetting>(payload.settings)
                |> Option.iter (PiView.ActionSettingReceived >> dispatch)
            | PiIn_DidReceiveGlobalSettings (settings) ->
                Domain.tryParse<Domain.GlobalSettings>(settings)
                |> Option.iter (PiView.GlobalSettingsReceived >> dispatch)
            | PiIn_SendToPropertyInspector _ -> 
                ()

            return! loop()
        }
        loop()
    )