module StreamDeck.Homebridge.PiAgent

open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel
open StreamDeck.Homebridge.PiModel

let createPiAgent(dispatch: PiMsg -> unit) : MailboxProcessor<PiEvent> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop() =
            async {
                let! msg = inbox.Receive()
                console.log($"PI message is: %A{msg}", msg)

                match msg with
                | PiEvent.Connected(startArgs, replyAgent) ->
                    replyAgent.Post <| PiCommand.GetGlobalSettings
                    dispatch <| PiMsg.PiConnected(startArgs, replyAgent)
                | PiEvent.DidReceiveSettings(_, payload) ->
                    Domain.tryParse<Domain.ActionSetting> payload.settings
                    |> Option.iter(PiMsg.ActionSettingReceived >> dispatch)
                | PiEvent.DidReceiveGlobalSettings settings ->
                    Domain.tryParse<Domain.GlobalSettings> settings
                    |> Option.iter(PiMsg.GlobalSettingsReceived >> dispatch)
                | PiEvent.DidReceivePluginMessage _ -> ()

                return! loop()
            }

        loop())
