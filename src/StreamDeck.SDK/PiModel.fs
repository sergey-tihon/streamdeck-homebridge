module StreamDeck.SDK.PiModel

open Browser.Dom
open Browser.Types
open Dto

[<RequireQualifiedAccess>]
type PiInEvent =
    /// When `websocket.onopen` this event diliver initial state to property inspector.
    | Connected of startArgs: StartArgs * replyAgent: MailboxProcessor<PiOutEvent>
    /// Event received after calling the getSettings API to retrieve the persistent data stored for the action.
    | DidReceiveSettings of event: Event * payload: ActionPayload
    /// Event received after calling the getGlobalSettings API to retrieve the global persistent data.
    | DidReceiveGlobalSettings of setting: obj
    /// Event received by the Property Inspector when the plugin uses the `sendToPropertyInspector` event.
    | SendToPropertyInspector of event: Event

and [<RequireQualifiedAccess>] PiOutEvent =
    /// Save data persistently for the action's instance.
    | SetSettings of payload: obj
    /// Request the persistent data for the action's instance.
    | GetSettings
    /// Save data securely and globally for the plugin.
    | SetGlobalSettings of payload: obj
    /// Request the global persistent data.
    | GetGlobalSettings
    /// Open an URL in the default browser.
    | OpenUrl of url: string
    /// Write a debug log to the logs file.
    | LogMessage of message: string
    /// Send a payload to the plugin.
    | SendToPlugin of payload: obj

let createReplyAgent (args: StartArgs) (websocket: WebSocket) : MailboxProcessor<PiOutEvent> =
    let inPropertyInspectorUUID = args.UUID

    MailboxProcessor.Start(fun inbox ->
        let sendJson(o: obj) =
            Utils.sendJson websocket o

        let rec loop() =
            async {
                let! msg = inbox.Receive()
                console.log($"PI sent event %A{msg}", msg)

                match msg with
                | PiOutEvent.SetSettings payload ->
                    sendJson {|
                        event = "setSettings"
                        // An opaque value identifying the Property Inspector. This value is received by the Property Inspector as parameter of the connectElgatoStreamDeckSocket function.
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}
                | PiOutEvent.GetSettings ->
                    sendJson {|
                        event = "getSettings"
                        // An opaque value identifying the Property Inspector. This value is received by the Property Inspector as parameter of the connectElgatoStreamDeckSocket function.
                        context = inPropertyInspectorUUID
                    |}
                | PiOutEvent.SetGlobalSettings payload ->
                    sendJson {|
                        event = "setGlobalSettings"
                        // An opaque value identifying the Property Inspector (inPropertyInspectorUUID). This value is received during the Registration procedure.
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}
                | PiOutEvent.GetGlobalSettings ->
                    sendJson {|
                        event = "getGlobalSettings"
                        // An opaque value identifying the Property Inspector (inPropertyInspectorUUID). This value is received during the Registration procedure.
                        context = inPropertyInspectorUUID
                    |}
                | PiOutEvent.OpenUrl url ->
                    sendJson {|
                        event = "openUrl"
                        payload = {| url = url |}
                    |}
                | PiOutEvent.LogMessage message ->
                    sendJson {|
                        event = "logMessage"
                        payload = {| message = message |}
                    |}
                | PiOutEvent.SendToPlugin payload ->
                    sendJson {|
                        action = args.ActionInfo.Value.action
                        event = "sendToPlugin"
                        // An opaque value identifying the Property Inspector. This value is received by the Property Inspector as parameter of the connectElgatoStreamDeckSocket function.
                        context = inPropertyInspectorUUID
                        payload = payload
                    |}

                return! loop()
            }

        loop())

let connectPropertyInspector (args: StartArgs) (agent: MailboxProcessor<PiInEvent>) =
    Utils.addDynamicStyles args.ApplicationInfo.colors

    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent args websocket

    websocket.onopen <-
        fun _ ->
            Utils.sendJson websocket {|
                event = "registerPropertyInspector"
                uuid = args.UUID
            |}

            agent.Post <| PiInEvent.Connected(args, replyAgent)

    websocket.onmessage <-
        fun messageEvent ->
            let event = (Utils.parseJson messageEvent.data) :?> Event
            let payload = event.payload :?> ActionPayload

            let piEvent =
                match event.event with
                | "didReceiveSettings" -> Some <| PiInEvent.DidReceiveSettings(event, payload)
                | "didReceiveGlobalSettings" -> Some <| PiInEvent.DidReceiveGlobalSettings(payload.settings)
                | "sendToPropertyInspector" -> Some <| PiInEvent.SendToPropertyInspector(event)
                | _ ->
                    console.warn($"Unexpected event ({event.event}) received by Property Inspector", event)
                    None

            piEvent |> Option.iter agent.Post
