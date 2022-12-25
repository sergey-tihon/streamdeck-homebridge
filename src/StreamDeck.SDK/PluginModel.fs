module StreamDeck.SDK.PluginModel

open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Dto

[<RequireQualifiedAccess>]
type PluginInEvent =
    /// When `websocket.onopen` this event diliver initial state to plugin.
    | Connected of startArgs: StartArgs * replyAgent: MailboxProcessor<PluginOutEvent>
    /// Event received after calling the getSettings API to retrieve the persistent data stored for the action.
    | DidReceiveSettings of event: Event * payload: ActionPayload
    /// Event received after calling the getGlobalSettings API to retrieve the global persistent data.
    | DidReceiveGlobalSettings of setting: obj
    /// When the user presses a key, the plugin will receive the `keyDown` event.
    | KeyDown of event: Event * payload: ActionPayload
    /// When the user releases a key, the plugin will receive the `keyUp` event.
    | KeyUp of event: Event * payload: ActionPayload
    // When the user touches the display, the plugin will receive the touchTap event (SD+).
    | TouchTap of event: Event * payload: TouchTapActionPayload
    // When the user presses or releases the encoder, the plugin will receive the dialPress event (SD+).
    | DialPress of event: Event * payload: DialPressActionPayload
    // When the user rotates the encoder, the plugin will receive the dialRotate event (SD+).
    | DialRotate of event: Event * payload: DialRotateActionPayload
    /// When an instance of an action is displayed on the Stream Deck, for example when the hardware is first plugged in, or when a folder containing that action is entered, the plugin will receive a `willAppear` event.
    | WillAppear of event: Event * payload: AppearanceActionPayload
    /// When an instance of an action ceases to be displayed on Stream Deck, for example when switching profiles or folders, the plugin will receive a `willDisappear` event.
    | WillDisappear of event: Event * payload: AppearanceActionPayload
    /// When the user changes the title or title parameters, the plugin will receive a `titleParametersDidChange` event.
    | TitleParametersDidChange of event: Event * payload: ActionTitlePayload
    /// When a device is plugged to the computer, the plugin will receive a `deviceDidConnect` event.
    | DeviceDidConnect of event: DeviceEvent
    /// When a device is unplugged from the computer, the plugin will receive a `deviceDidDisconnect` event.
    | DeviceDidDisconnect of event: DeviceEvent
    /// When a monitored application is launched, the plugin will be notified and will receive the `applicationDidLaunch` event.
    | ApplicationDidLaunch of applicationId: string
    /// When a monitored application is terminated, the plugin will be notified and will receive the `applicationDidTerminate` event.
    | ApplicationDidTerminate of applicationId: string
    /// When the computer is wake up, the plugin will be notified and will receive the `systemDidWakeUp` event.
    | SystemDidWakeUp
    /// Event received when the Property Inspector appears in the Stream Deck software user interface, for example when selecting a new instance.
    | PropertyInspectorDidAppear of event: Event
    /// Event received when the Property Inspector for an instance is removed from the Stream Deck software user interface, for example when selecting a different instance.
    | PropertyInspectorDidDisappear of event: Event
    /// Event received by the plugin when the Property Inspector uses the `sendToPlugin` event.
    | SendToPlugin of event: Event

and [<RequireQualifiedAccess>] PluginOutEvent =
    /// Save data persistently for the action's instance.
    | SetSettings of context: string * payload: obj
    /// Request the persistent data for the action's instance.
    | GetSettings of context: string
    /// Save data securely and globally for the plugin.
    | SetGlobalSettings of payload: obj
    /// Request the global persistent data.
    | GetGlobalSettings
    /// Open an URL in the default browser.
    | OpenUrl of url: string
    /// Write a debug log to the logs file.
    | LogMessage of message: string
    /// Dynamically change the title of an instance of an action.
    | SetTitle of context: string * payload: SetTitlePayload
    /// Dynamically change the image displayed by an instance of an action.
    | SetImage of context: string * payload: SetImagePayload
    // Dynamically change properties of items on the Stream Deck + touch display.
    | SetFeedback of context: string * payload: Dictionary<string, obj>
    // Dynamically change the current layout for the Stream Deck + touch display
    | SetFeedbackLayout of context: string * payload: SetFeedbackLayoutPayload
    /// Temporarily show an alert icon on the image displayed by an instance of an action.
    | ShowAlert of context: string
    /// Temporarily show an OK checkmark icon on the image displayed by an instance of an action.
    | ShowOk of context: string
    /// Change the state of the action's instance supporting multiple states.
    | SetState of context: string * state: int
    /// Switch to one of the preconfigured read-only profiles.
    | SwitchToProfile of device: string * profileName: string
    /// Send a payload to the Property Inspector.
    | SendToPropertyInspector of context: string * action: string * payload: obj

let createReplyAgent (args: StartArgs) (websocket: WebSocket) : MailboxProcessor<PluginOutEvent> =
    let inPluginUUID = args.UUID

    MailboxProcessor.Start(fun inbox ->
        let sendJson(o: obj) =
            Utils.sendJson websocket o

        let rec loop() = async {
            let! msg = inbox.Receive()
            console.log($"Plugin sent event %A{msg}", msg)

            match msg with
            | PluginOutEvent.SetSettings(context, payload) ->
                sendJson
                    {|
                        event = "setSettings"
                        // An opaque value identifying the instance's action.
                        context = context
                        payload = payload
                    |}
            | PluginOutEvent.GetSettings context ->
                sendJson
                    {|
                        event = "getSettings"
                        // An opaque value identifying the instance's action
                        context = context
                    |}
            | PluginOutEvent.SetGlobalSettings(payload) ->
                sendJson
                    {|
                        event = "setGlobalSettings"
                        // An opaque value identifying the plugin (inPluginUUID). This value is received during the Registration procedure.
                        context = inPluginUUID
                        payload = payload
                    |}
            | PluginOutEvent.GetGlobalSettings ->
                sendJson
                    {|
                        event = "getGlobalSettings"
                        // An opaque value identifying the plugin (inPluginUUID). This value is received during the Registration procedure.
                        context = inPluginUUID
                    |}
            | PluginOutEvent.OpenUrl url ->
                sendJson
                    {|
                        event = "openUrl"
                        payload = {| url = url |}
                    |}
            | PluginOutEvent.LogMessage message ->
                sendJson
                    {|
                        event = "logMessage"
                        payload = {| message = message |}
                    |}
            | PluginOutEvent.SetTitle(context, payload) ->
                sendJson
                    {|
                        event = "setTitle"
                        // An opaque value identifying the instance's action you want to modify.
                        context = context
                        payload = payload
                    |}
            | PluginOutEvent.SetImage(context, payload) ->
                sendJson
                    {|
                        event = "setImage"
                        // An opaque value identifying the instance's action you want to modify.
                        context = context
                        payload = payload
                    |}
            | PluginOutEvent.SetFeedback(context, payload) ->
                sendJson
                    {|
                        event = "setFeedback"
                        // A value to Identify the instance's action you want to modify.
                        context = context
                        payload = payload
                    |}
            | PluginOutEvent.SetFeedbackLayout(context, payload) ->
                sendJson
                    {|
                        event = "setFeedbackLayout"
                        context = context
                        payload = payload
                    |}
            | PluginOutEvent.ShowAlert context ->
                sendJson
                    {|
                        event = "showAlert"
                        // An opaque value identifying the instance's action.
                        context = context
                    |}
            | PluginOutEvent.ShowOk context ->
                sendJson
                    {|
                        event = "showOk"
                        // An opaque value identifying the instance's action.
                        context = context
                    |}
            | PluginOutEvent.SetState(context, state) ->
                sendJson
                    {|
                        event = "setState"
                        // An opaque value identifying the instance's action.
                        context = context
                        payload = {| state = state |}
                    |}
            | PluginOutEvent.SwitchToProfile(device, profileName) ->
                sendJson
                    {|
                        event = "switchToProfile"
                        // An opaque value identifying the plugin. This value should be set to the PluginUUID received during the registration procedure.
                        context = inPluginUUID
                        device = device
                        payload = {| profile = profileName |}
                    |}
            | PluginOutEvent.SendToPropertyInspector(context, action, payload) ->
                sendJson
                    {|
                        action = action
                        event = "sendToPropertyInspector"
                        // An opaque value identifying the instance's action.
                        context = context
                        payload = payload
                    |}

            return! loop()
        }

        loop())

let connectPlugin (args: Dto.StartArgs) (agent: MailboxProcessor<PluginInEvent>) =
    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent args websocket

    websocket.onopen <-
        fun _ ->
            Utils.sendJson
                websocket
                {|
                    event = "registerPlugin"
                    uuid = args.UUID
                |}

            agent.Post <| PluginInEvent.Connected(args, replyAgent)

    websocket.onmessage <-
        fun messageEvent ->
            let json = Utils.parseJson messageEvent.data
            let event = json :?> Event
            let payload = event.payload :?> ActionPayload

            let pEvent =
                match event.event with
                | "didReceiveSettings" -> Some(PluginInEvent.DidReceiveSettings(event, payload))
                | "didReceiveGlobalSettings" -> Some(PluginInEvent.DidReceiveGlobalSettings(payload.settings))
                | "keyDown" -> Some(PluginInEvent.KeyDown(event, payload))
                | "keyUp" -> Some(PluginInEvent.KeyUp(event, payload))
                | "touchTap" -> Some(PluginInEvent.TouchTap(event, event.payload :?> TouchTapActionPayload))
                | "dialPress" -> Some(PluginInEvent.DialPress(event, event.payload :?> DialPressActionPayload))
                | "dialRotate" -> Some(PluginInEvent.DialRotate(event, event.payload :?> DialRotateActionPayload))
                | "willAppear" -> Some(PluginInEvent.WillAppear(event, event.payload :?> AppearanceActionPayload))
                | "willDisappear" -> Some(PluginInEvent.WillDisappear(event, event.payload :?> AppearanceActionPayload))
                | "titleParametersDidChange" ->
                    Some(PluginInEvent.TitleParametersDidChange(event, event.payload :?> ActionTitlePayload))
                | "deviceDidConnect" -> Some(PluginInEvent.DeviceDidConnect(json :?> DeviceEvent))
                | "deviceDidDisconnect" -> Some(PluginInEvent.DeviceDidDisconnect(json :?> DeviceEvent))
                | "applicationDidLaunch" ->
                    Some(PluginInEvent.ApplicationDidLaunch((json :?> ApplicationPayload).application))
                | "applicationDidTerminate" ->
                    Some(PluginInEvent.ApplicationDidTerminate((json :?> ApplicationPayload).application))
                | "systemDidWakeUp" -> Some(PluginInEvent.SystemDidWakeUp)
                | "propertyInspectorDidAppear" -> Some(PluginInEvent.PropertyInspectorDidAppear(event))
                | "propertyInspectorDidDisappear" -> Some(PluginInEvent.PropertyInspectorDidDisappear(event))
                | "sendToPlugin" -> Some(PluginInEvent.SendToPlugin(event))
                | _ ->
                    console.warn($"Unexpected event ({event.event}) received by Plugin", event)
                    None

            pEvent |> Option.iter agent.Post
