module StreamDeck.SDK.Plugin

open Browser.Dom
open Browser.Types
open Dto

type PluginIn_Events =
    /// When `websocket.onopen` this event diliver initial state to plugin.
    | PluginIn_Connected of startArgs:StartArgs * replyAgent:MailboxProcessor<PluginOut_Events>
    /// Event received after calling the getSettings API to retrieve the persistent data stored for the action.
    | PluginIn_DidReceiveSettings of event:Event * payload: ActionPayload
    /// Event received after calling the getGlobalSettings API to retrieve the global persistent data.
    | PluginIn_DidReceiveGlobalSettings of setting:obj
    /// When the user presses a key, the plugin will receive the `keyDown` event.
    | PluginIn_KeyDown of event:Event * payload: ActionPayload
    /// When the user releases a key, the plugin will receive the `keyUp` event.
    | PluginIn_KeyUp of event:Event * payload: ActionPayload
    /// When an instance of an action is displayed on the Stream Deck, for example when the hardware is first plugged in, or when a folder containing that action is entered, the plugin will receive a `willAppear` event.
    | PluginIn_WillAppear of event:Event * payload: ActionPayload
    /// When an instance of an action ceases to be displayed on Stream Deck, for example when switching profiles or folders, the plugin will receive a `willDisappear` event.
    | PluginIn_WillDisappear of event:Event * payload: ActionPayload
    /// When the user changes the title or title parameters, the plugin will receive a `titleParametersDidChange` event.
    | PluginIn_TitleParametersDidChange of event:Event * payload:ActionTitlePayload
    /// When a device is plugged to the computer, the plugin will receive a `deviceDidConnect` event.
    | PluginIn_DeviceDidConnect of event:DeviceEvent
    /// When a device is unplugged from the computer, the plugin will receive a `deviceDidDisconnect` event.
    | PluginIn_DeviceDidDisconnect of event:DeviceEvent
    /// When a monitored application is launched, the plugin will be notified and will receive the `applicationDidLaunch` event.
    | PluginIn_ApplicationDidLaunch of applicationId: string
    /// When a monitored application is terminated, the plugin will be notified and will receive the `applicationDidTerminate` event.
    | PluginIn_ApplicationDidTerminate of applicationId: string
    /// When the computer is wake up, the plugin will be notified and will receive the `systemDidWakeUp` event.
    | PluginIn_SystemDidWakeUp
    /// Event received when the Property Inspector appears in the Stream Deck software user interface, for example when selecting a new instance.
    | PluginIn_PropertyInspectorDidAppear of event:Event 
    /// Event received when the Property Inspector for an instance is removed from the Stream Deck software user interface, for example when selecting a different instance.
    | PluginIn_PropertyInspectorDidDisappear of event:Event 
    /// Event received by the plugin when the Property Inspector uses the `sendToPlugin` event.
    | PluginIn_SendToPlugin of event:Event 

and PluginOut_Events =
    /// Save data persistently for the action's instance.
    | PluginOut_SetSettings of context:string * payload:obj
    /// Request the persistent data for the action's instance.
    | PluginOut_GetSettings of context:string
    /// Save data securely and globally for the plugin.
    | PluginOut_SetGlobalSettings of context:string * payload:obj
    /// Request the global persistent data.
    | PluginOut_GetGlobalSettings of context:string
    /// Open an URL in the default browser.
    | PluginOut_OpenUrl of url:string
    /// Write a debug log to the logs file.
    | PluginOut_LogMessage of message:string
    /// Dynamically change the title of an instance of an action.
    | PluginOut_SetTitle of context:string * payload:SetTitlePayload
    /// Dynamically change the image displayed by an instance of an action.
    | PluginOut_SetImage of context:string * payload:SetImagePayload
    /// Temporarily show an alert icon on the image displayed by an instance of an action.
    | PluginOut_ShowAlert of context:string
    /// Temporarily show an OK checkmark icon on the image displayed by an instance of an action.
    | PluginOut_ShowOk of context:string
    /// Change the state of the action's instance supporting multiple states.
    | PluginOut_SetState of context:string * state:int
    /// Switch to one of the preconfigured read-only profiles.
    | PluginOut_SwitchToProfile of context:string * device:string * profileName:string
    /// Send a payload to the Property Inspector.
    | PluginOut_SendToPropertyInspector of context:string * action:string * payload:obj

let createReplyAgent (websocket:WebSocket) :MailboxProcessor<PluginOut_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec loop() = async{
            match! inbox.Receive() with
            | PluginOut_SetSettings(context, payload) ->
                websocket.send {|
                    event = "setSettings"
                    context = context
                    payload = payload
                |}
            | PluginOut_GetSettings context ->
                websocket.send {|
                    event = "getSettings"
                    context = context
                |}
            | PluginOut_SetGlobalSettings(context, payload) ->
                websocket.send {|
                    event = "setGlobalSettings"
                    context = context
                    payload = payload
                |}
            | PluginOut_GetGlobalSettings context ->
                websocket.send {|
                    event = "getGlobalSettings"
                    context = context
                |}
            | PluginOut_OpenUrl url ->
                websocket.send {|
                    event = "openUrl"
                    payload = {|
                        url =  url
                    |}
                |}
            | PluginOut_LogMessage message ->
                websocket.send {|
                    event = "logMessage"
                    payload = {|
                        message =  message
                    |}
                |}
            | PluginOut_SetTitle(context, payload) ->
                websocket.send {|
                    event = "setTitle"
                    context = context
                    payload = payload
                |}
            | PluginOut_SetImage(context, payload) ->
                websocket.send {|
                    event = "setImage"
                    context = context
                    payload = payload
                |}
            | PluginOut_ShowAlert context ->
                websocket.send {|
                    event = "showAlert"
                    context = context
                |}
            | PluginOut_ShowOk context ->
                websocket.send {|
                    event = "showOk"
                    context = context
                |}
            | PluginOut_SetState(context, state) ->
                websocket.send {|
                    event = "setState"
                    context = context
                    payload = {|
                        state =  state
                    |}
                |}
            | PluginOut_SwitchToProfile(context, device, profileName) ->
                websocket.send {|
                    event = "switchToProfile"
                    context = context
                    device = device
                    payload = {|
                        profile =  profileName
                    |}
                |}
            | PluginOut_SendToPropertyInspector(context, action, payload) ->
                websocket.send {|
                    action = action
                    event = "sendToPropertyInspector"
                    context = context
                    payload = payload
                |}
            return! loop()
        }
        loop()
    )

let connectPlugin (args:Dto.StartArgs) (agent:MailboxProcessor<PluginIn_Events>) =
    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent websocket

    websocket.onopen <- fun _ -> 
        websocket.send {| 
            event = "registerPlugin"
            uuid = args.UUID
        |}
        agent.Post <| PluginIn_Connected(args,replyAgent)

    websocket.onmessage <- fun messageEvent -> 
        let event = messageEvent.data :?> Event
        let payload = event.payload :?> ActionPayload
        let pEvent =
            match event.event with
            | "didReceiveSettings"              -> Some <| PluginIn_DidReceiveSettings(event, payload)
            | "didReceiveGlobalSettings"        -> Some <| PluginIn_DidReceiveGlobalSettings(payload.settings)
            | "keyDown"                         -> Some <| PluginIn_KeyDown(event, payload)
            | "keyUp"                           -> Some <| PluginIn_KeyDown(event, payload)
            | "willAppear"                      -> Some <| PluginIn_WillAppear(event, payload)
            | "willDisappear"                   -> Some <| PluginIn_WillDisappear(event, payload)
            | "titleParametersDidChange"        -> Some <| PluginIn_TitleParametersDidChange(event, event.payload :?> ActionTitlePayload)
            | "deviceDidConnect"                -> Some <| PluginIn_DeviceDidConnect(messageEvent.data :?> DeviceEvent)
            | "deviceDidDisconnect"             -> Some <| PluginIn_DeviceDidDisconnect(messageEvent.data :?> DeviceEvent)
            | "applicationDidLaunch"            -> Some <| PluginIn_ApplicationDidLaunch((messageEvent.data :?> ApplicationPayload).application)
            | "applicationDidTerminate"         -> Some <| PluginIn_ApplicationDidTerminate((messageEvent.data :?> ApplicationPayload).application)
            | "systemDidWakeUp"                 -> Some <| PluginIn_SystemDidWakeUp
            | "propertyInspectorDidAppear"      -> Some <| PluginIn_PropertyInspectorDidAppear(event)
            | "propertyInspectorDidDisappear"   -> Some <| PluginIn_PropertyInspectorDidDisappear(event)
            | "sendToPlugin"                    -> Some <| PluginIn_SendToPlugin(event)
            | _ -> 
                console.warn($"Unexpected event ({event.event}) received by Plugin")
                None
        pEvent |> Option.iter agent.Post