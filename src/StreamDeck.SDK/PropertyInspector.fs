module StreamDeck.SDK.PropertyInspector

open Browser.Dom
open Browser.Types
open Dto

type PiIn_Events =
    /// When `websocket.onopen` this event diliver initial state to property inspector.
    | PiIn_Connected of startArgs:StartArgs * replyAgent:MailboxProcessor<PiOut_Events>
    /// Event received after calling the getSettings API to retrieve the persistent data stored for the action.
    | PiIn_DidReceiveSettings of event:Event * payload: ActionPayload
    /// Event received after calling the getGlobalSettings API to retrieve the global persistent data.
    | PiIn_DidReceiveGlobalSettings of setting:obj
    /// Event received by the Property Inspector when the plugin uses the `sendToPropertyInspector` event.
    | PiIn_SendToPropertyInspector of event:Event

and PiOut_Events =
    /// Save data persistently for the action's instance.
    | PiOut_SetSettings of payload:obj
    /// Request the persistent data for the action's instance.
    | PiOut_GetSettings
    /// Save data securely and globally for the plugin.
    | PiOut_SetGlobalSettings of payload:obj
    /// Request the global persistent data.
    | PiOut_GetGlobalSettings
    /// Open an URL in the default browser.
    | PiOut_OpenUrl of url:string
    /// Write a debug log to the logs file.
    | PiOut_LogMessage of message:string
    /// Send a payload to the plugin.
    | PiOut_SendToPlugin of payload:obj

let createReplyAgent (args:StartArgs) (websocket:WebSocket) :MailboxProcessor<PiOut_Events> = 
    let context = args.ActionInfo.Value.context
    let action = args.ActionInfo.Value.action
    MailboxProcessor.Start(fun inbox->
        let rec loop() = async{
            match! inbox.Receive() with
            | PiOut_SetSettings payload ->
                websocket.send {|
                    event = "setSettings"
                    context = context
                    payload = payload
                |}
            | PiOut_GetSettings ->
                websocket.send {|
                    event = "getSettings"
                    context = context
                |}
            | PiOut_SetGlobalSettings payload ->
                websocket.send {|
                    event = "setGlobalSettings"
                    context = context
                    payload = payload
                |}
            | PiOut_GetGlobalSettings ->
                websocket.send {|
                    event = "getGlobalSettings"
                    context = context
                |}
            | PiOut_OpenUrl url ->
                websocket.send {|
                    event = "openUrl"
                    payload = {|
                        url =  url
                    |}
                |}
            | PiOut_LogMessage message ->
                websocket.send {|
                    event = "logMessage"
                    payload = {|
                        message =  message
                    |}
                |}
            | PiOut_SendToPlugin payload ->
                websocket.send {|
                    action = action
                    event = "sendToPlugin"
                    context = context
                    payload = payload
                |}
            return! loop()
        }
        loop()
    )

let connectPropertyInspector (args:StartArgs) (agent:MailboxProcessor<PiIn_Events>) =
    let websocket = Utils.createWebSocket args.Port
    let replyAgent = createReplyAgent args websocket

    websocket.onopen <- fun _ -> 
        websocket.send {| 
            event = "registerPropertyInspector"
            uuid = args.UUID
        |}
        agent.Post <| PiIn_Connected (args,replyAgent)

    websocket.onmessage <- fun messageEvent -> 
        let event = messageEvent.data :?> Event
        let payload = event.payload :?> ActionPayload
        let piEvent = 
            match event.event with
            | "didReceiveSettings"          -> Some <| PiIn_DidReceiveSettings(event, payload)
            | "didReceiveGlobalSettings"    -> Some <| PiIn_DidReceiveGlobalSettings(payload.settings)
            | "sendToPropertyInspector"     -> Some <| PiIn_SendToPropertyInspector(event)
            | _ -> 
                console.warn($"Unexpected event ({event.event}) received by Property Inspector")
                None
        piEvent |> Option.iter agent.Post
