module StreamDeck.SDK.PluginModel

open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Dto

[<RequireQualifiedAccess>]
type PluginEvent =
    | Connected of startArgs: StartArgs * replyAgent: MailboxProcessor<PluginCommand>
    /// Occurs when a monitored application is launched. Monitored applications can be defined in the manifest.json file via the Manifest.ApplicationsToMonitor property. See also ApplicationDidTerminate.
    | ApplicationDidLaunch of applicationId: string
    /// Occurs when a monitored application terminates. Monitored applications can be defined in the manifest.json file via the Manifest.ApplicationsToMonitor property. See also ApplicationDidLaunch.
    | ApplicationDidTerminate of applicationId: string
    /// Occurs when a Stream Deck device is connected. See also DeviceDidDisconnect.
    | DeviceDidConnect of event: DeviceEvent
    /// Occurs when a Stream Deck device is disconnected. See also DeviceDidConnect.
    | DeviceDidDisconnect of event: DeviceEvent
    /// Occurs when the user presses a dial (Stream Deck +). See also DialUp.
    | DialDown of event: Event * payload: EncoderPayload
    /// Occurs when the user rotates a dial (Stream Deck +).
    | DialRotate of event: Event * payload: DialRotatePayload
    /// Occurs when the user releases a pressed dial (Stream Deck +). See also DialDown.
    | DialUp of event: Event * payload: EncoderPayload
    /// Occurs when Stream Deck receives a deep-link message intended for the plugin. The message is re-routed to the plugin, and provided as part of the payload. One-way deep-link message can be routed to the plugin using the URL format streamdeck://plugins/message/<PLUGIN_UUID>/{MESSAGE}.
    | DidReceiveDeepLink of url: string
    /// Occurs when the plugin receives the global settings from the Stream Deck.
    | DidReceiveGlobalSettings of setting: obj
    /// Occurs when a payload was received from the UI.
    | DidReceivePropertyInspectorMessage of event: Event
    /// Occurs when the settings associated with an action instance are requested, or when the the settings were updated by the property inspector.
    | DidReceiveSettings of event: Event * payload: ActionPayload
    /// Occurs when the user presses a action down. See also KeyUp.
    | KeyDown of event: Event * payload: ActionPayload
    /// Occurs when the user releases a pressed action. See also KeyDown.
    | KeyUp of event: Event * payload: ActionPayload
    /// Occurs when the property inspector associated with the action becomes visible, i.e. the user selected an action in the Stream Deck application. See also PropertyInspectorDidDisappear.
    | PropertyInspectorDidAppear of event: Event
    /// Occurs when the property inspector associated with the action becomes invisible, i.e. the user unselected the action in the Stream Deck application. See also PropertyInspectorDidAppear.
    | PropertyInspectorDidDisappear of event: Event
    /// Occurs when the computer wakes up.
    | SystemDidWakeUp
    /// Occurs when the user updates an action's title settings in the Stream Deck application.
    | TitleParametersDidChange of event: Event * payload: ActionTitlePayload
    /// Occurs when the user taps the touchscreen (Stream Deck +).
    | TouchTap of event: Event * payload: TouchTapActionPayload
    /// Occurs when an action appears on the Stream Deck due to the user navigating to another page, profile, folder, etc. This also occurs during startup if the action is on the "front page". An action refers to all types of actions, e.g. keys, dials, touchscreens, pedals, etc.
    | WillAppear of event: Event * payload: AppearanceActionPayload
    /// Occurs when an action disappears from the Stream Deck due to the user navigating to another page, profile, folder, etc. An action refers to all types of actions, e.g. keys, dials, touchscreens, pedals, etc.
    | WillDisappear of event: Event * payload: AppearanceActionPayload

and [<RequireQualifiedAccess>] PluginCommand =
    /// Gets the global settings associated with the plugin. Causes DidReceiveGlobalSettings to be emitted.
    | GetGlobalSettings
    /// Gets the settings associated with an instance of an action. Causes DidReceiveSettings to be emitted.
    | GetSettings of context: string
    /// Logs a message to the file-system.
    | LogMessage of message: string
    /// Opens the URL in the user's default browser.
    | OpenUrl of url: string
    /// Sends a message to the property inspector.
    | SendToPropertyInspector of context: string * payload: obj
    /// Set's the feedback of an existing layout associated with an action instance.
    | SetFeedback of context: string * payload: Dictionary<string, obj>
    /// Sets the layout associated with an action instance.
    | SetFeedbackLayout of context: string * payload: SetFeedbackLayoutPayload
    /// Sets the global settings associated with the plugin.
    | SetGlobalSettings of context: string * payload: obj
    /// Sets the image associated with an action instance.
    | SetImage of context: string * payload: SetImagePayload
    /// Sets the settings associated with an instance of an action.
    | SetSettings of context: string * payload: obj
    /// Sets the settings associated with an instance of an action.
    | SetState of context: string * state: int
    /// Sets the title displayed for an instance of an action.
    | SetTitle of context: string * payload: SetTitlePayload
    /// Sets the trigger descriptions associated with an encoder action instance.
    | SetTriggerDescription of context: string * payloaf: SetTriggerDescriptionPayload
    /// Temporarily shows an alert (i.e. warning), in the form of an exclamation mark in a yellow triangle, on the action instance. Used to provide visual feedback when an action failed.
    | ShowAlert of context: string
    /// Temporarily shows an "OK" (i.e. success), in the form of a check-mark in a green circle, on the action instance. Used to provide visual feedback when an action successfully executed.
    | ShowOk of context: string
    /// Switches to the profile, as distributed by the plugin, on the specified device.
    | SwitchToProfile of device: string * profileName: string * page: string

let createReplyAgent (args: StartArgs) (websocket: WebSocket) : MailboxProcessor<PluginCommand> =
    let inPluginUUID = args.UUID

    MailboxProcessor.Start(fun inbox ->
        let sendJson(o: obj) =
            Utils.sendJson websocket o

        let rec loop() =
            async {
                let! msg = inbox.Receive()
                console.log($"Plugin sent event %A{msg}", msg)

                match msg with
                | PluginCommand.GetGlobalSettings ->
                    sendJson {|
                        event = "getGlobalSettings"
                        context = inPluginUUID
                    |}
                | PluginCommand.GetSettings context ->
                    sendJson {|
                        event = "getSettings"
                        context = context
                    |}
                | PluginCommand.OpenUrl url ->
                    sendJson {|
                        event = "openUrl"
                        payload = {| url = url |}
                    |}
                | PluginCommand.LogMessage message ->
                    sendJson {|
                        event = "logMessage"
                        payload = {| message = message |}
                    |}
                | PluginCommand.SendToPropertyInspector(context, payload) ->
                    sendJson {|
                        event = "sendToPropertyInspector"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetFeedback(context, payload) ->
                    sendJson {|
                        event = "setFeedback"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetFeedbackLayout(context, payload) ->
                    sendJson {|
                        event = "setFeedbackLayout"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetGlobalSettings(context, payload) ->
                    sendJson {|
                        event = "setGlobalSettings"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetImage(context, payload) ->
                    sendJson {|
                        event = "setImage"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetSettings(context, payload) ->
                    sendJson {|
                        event = "setSettings"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetState(context, state) ->
                    sendJson {|
                        event = "setState"
                        context = context
                        payload = {| state = state |}
                    |}
                | PluginCommand.SetTitle(context, payload) ->
                    sendJson {|
                        event = "setTitle"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.SetTriggerDescription(context, payload) ->
                    sendJson {|
                        event = "setTriggerDescription"
                        context = context
                        payload = payload
                    |}
                | PluginCommand.ShowAlert context ->
                    sendJson {|
                        event = "showAlert"
                        context = context
                    |}
                | PluginCommand.ShowOk context ->
                    sendJson {|
                        event = "showOk"
                        context = context
                    |}
                | PluginCommand.SwitchToProfile(device, profile, page) ->
                    sendJson {|
                        event = "switchToProfile"
                        context = inPluginUUID
                        device = device
                        payload = {| profile = profile; page = page |}
                    |}

                return! loop()
            }

        loop())

let connectPlugin (args: Dto.StartArgs) (agent: MailboxProcessor<PluginEvent>) =
    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent args websocket

    websocket.onopen <-
        fun _ ->
            Utils.sendJson websocket {|
                event = "registerPlugin"
                uuid = args.UUID
            |}

            agent.Post <| PluginEvent.Connected(args, replyAgent)

    websocket.onmessage <-
        fun messageEvent ->
            let json = Utils.parseJson messageEvent.data
            let event = json :?> Event
            let payload = event.payload :?> ActionPayload

            let pEvent =
                match event.event with
                | "didReceiveSettings" -> Some(PluginEvent.DidReceiveSettings(event, payload))
                | "didReceiveGlobalSettings" -> Some(PluginEvent.DidReceiveGlobalSettings(payload.settings))
                | "keyDown" -> Some(PluginEvent.KeyDown(event, payload))
                | "keyUp" -> Some(PluginEvent.KeyUp(event, payload))
                | "touchTap" -> Some(PluginEvent.TouchTap(event, event.payload :?> TouchTapActionPayload))
                | "dialPress" -> Some(PluginEvent.DialDown(event, event.payload :?> EncoderPayload))
                | "dialRotate" -> Some(PluginEvent.DialRotate(event, event.payload :?> DialRotatePayload))
                | "willAppear" -> Some(PluginEvent.WillAppear(event, event.payload :?> AppearanceActionPayload))
                | "willDisappear" -> Some(PluginEvent.WillDisappear(event, event.payload :?> AppearanceActionPayload))
                | "titleParametersDidChange" ->
                    Some(PluginEvent.TitleParametersDidChange(event, event.payload :?> ActionTitlePayload))
                | "deviceDidConnect" -> Some(PluginEvent.DeviceDidConnect(json :?> DeviceEvent))
                | "deviceDidDisconnect" -> Some(PluginEvent.DeviceDidDisconnect(json :?> DeviceEvent))
                | "applicationDidLaunch" ->
                    Some(PluginEvent.ApplicationDidLaunch((json :?> ApplicationPayload).application))
                | "applicationDidTerminate" ->
                    Some(PluginEvent.ApplicationDidTerminate((json :?> ApplicationPayload).application))
                | "systemDidWakeUp" -> Some(PluginEvent.SystemDidWakeUp)
                | "propertyInspectorDidAppear" -> Some(PluginEvent.PropertyInspectorDidAppear(event))
                | "propertyInspectorDidDisappear" -> Some(PluginEvent.PropertyInspectorDidDisappear(event))
                | "sendToPlugin" -> Some(PluginEvent.DidReceivePropertyInspectorMessage(event))
                | _ ->
                    console.warn($"Unexpected event ({event.event}) received by Plugin", event)
                    None

            pEvent |> Option.iter agent.Post
