module StreamDeck.Homebridge.PiAgent

open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel

let createPiAgent(dispatch: PiView.PiMsg -> unit) : MailboxProcessor<PiInEvent> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop() = async {
            let! msg = inbox.Receive()
            console.log($"PI message is: %A{msg}", msg)

            match msg with
            | PiInEvent.Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PiOutEvent.GetGlobalSettings
                dispatch <| PiView.PiConnected(startArgs, replyAgent)
            | PiInEvent.DidReceiveSettings(event, payload) ->
                Domain.tryParse<Domain.ActionSetting>(payload.settings)
                |> Option.iter(PiView.ActionSettingReceived >> dispatch)
            | PiInEvent.DidReceiveGlobalSettings(settings) ->
                Domain.tryParse<Domain.GlobalSettings>(settings)
                |> Option.iter(PiView.GlobalSettingsReceived >> dispatch)
            | PiInEvent.SendToPropertyInspector _ -> ()

            return! loop()
        }

        loop())
