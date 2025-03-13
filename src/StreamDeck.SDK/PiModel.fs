module StreamDeck.SDK.PiModel

open Browser.Dom
open Browser.Types
open Dto

[<RequireQualifiedAccess>]
type PiEvent =
    /// When `websocket.onopen` this event diliver initial state to property inspector.
    | Connected of startArgs: StartArgs * replyAgent: MailboxProcessor<PiCommand>
    /// Occurs when the plugin receives the global settings from the Stream Deck.
    | DidReceiveGlobalSettings of setting: obj
    /// Occurs when a message was received from the plugin.
    | DidReceivePluginMessage of event: Event
    /// Occurs when the settings associated with an action instance are requested, or when the the settings were updated by the property inspector.
    | DidReceiveSettings of event: Event * payload: ActionPayload

and [<RequireQualifiedAccess>] PiCommand =
    /// Gets the global settings associated with the plugin. Causes DidReceiveGlobalSettings to be emitted.
    | GetGlobalSettings
    /// Gets the settings associated with an instance of an action. Causes DidReceiveSettings to be emitted.
    | GetSettings
    /// Opens the URL in the user's default browser.
    | OpenUrl of url: string
    /// Sends a message to the plugin.
    | SendToPlugin of payload: obj
    /// Sets the global settings associated with the plugin.
    | SetGlobalSettings of payload: obj
    /// Sets the settings associated with an instance of an action.
    | SetSettings of payload: obj

let createReplyAgent (args: StartArgs) (websocket: WebSocket) : MailboxProcessor<PiCommand> =
    let inPropertyInspectorUUID = args.UUID

    MailboxProcessor.Start(fun inbox ->
        let sendJson(o: obj) =
            Utils.sendJson websocket o

        let rec loop() =
            async {
                let! msg = inbox.Receive()
                console.log($"PI sent event %A{msg}", msg)

                match msg with
                | PiCommand.GetGlobalSettings ->
                    sendJson {|
                        event = "getGlobalSettings"
                        context = inPropertyInspectorUUID
                    |}
                | PiCommand.GetSettings ->
                    sendJson {|
                        action = args.ActionInfo.Value.action
                        event = "getSettings"
                        context = inPropertyInspectorUUID
                    |}
                | PiCommand.OpenUrl url ->
                    sendJson {|
                        event = "openUrl"
                        payload = {| url = url |}
                    |}
                | PiCommand.SendToPlugin payload ->
                    sendJson {|
                        action = args.ActionInfo.Value.action
                        event = "sendToPlugin"
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}
                | PiCommand.SetGlobalSettings payload ->
                    sendJson {|
                        event = "setGlobalSettings"
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}
                | PiCommand.SetSettings payload ->
                    sendJson {|
                        action = args.ActionInfo.Value.action
                        event = "setSettings"
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}

                return! loop()
            }

        loop())

let connectPropertyInspector (args: StartArgs) (agent: MailboxProcessor<PiEvent>) =
    Utils.addDynamicStyles args.ApplicationInfo.colors

    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent args websocket

    websocket.onopen <-
        fun _ ->
            Utils.sendJson websocket {|
                event = "registerPropertyInspector"
                uuid = args.UUID
            |}

            agent.Post <| PiEvent.Connected(args, replyAgent)

    websocket.onmessage <-
        fun messageEvent ->
            let event = Utils.parseJson messageEvent.data :?> Event
            let payload = event.payload :?> ActionPayload

            let piEvent =
                match event.event with
                | "didReceiveGlobalSettings" -> Some <| PiEvent.DidReceiveGlobalSettings payload.settings
                | "sendToPropertyInspector" -> Some <| PiEvent.DidReceivePluginMessage event
                | "didReceiveSettings" -> Some <| PiEvent.DidReceiveSettings(event, payload)
                | _ ->
                    console.warn($"Unexpected event ({event.event}) received by Property Inspector", event)
                    None

            piEvent |> Option.iter agent.Post
