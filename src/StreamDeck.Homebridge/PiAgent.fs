module StreamDeck.Homebridge.PiAgent

open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel
open StreamDeck.Homebridge.PiView

let createPiAgent(dispatch: PiMsg -> unit) : MailboxProcessor<PiInEvent> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop() = async {
            let! msg = inbox.Receive()
            console.log($"PI message is: %A{msg}", msg)

            match msg with
            | PiInEvent.Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PiOutEvent.GetGlobalSettings
                dispatch <| PiMsg.PiConnected(startArgs, replyAgent)
            | PiInEvent.DidReceiveSettings(event, payload) ->
                Domain.tryParse<Domain.ActionSetting>(payload.settings)
                |> Option.iter(PiMsg.ActionSettingReceived >> dispatch)
            | PiInEvent.DidReceiveGlobalSettings(settings) ->
                Domain.tryParse<Domain.GlobalSettings>(settings)
                |> Option.iter(PiMsg.GlobalSettingsReceived >> dispatch)
            | PiInEvent.SendToPropertyInspector _ -> ()

            return! loop()
        }

        loop())
