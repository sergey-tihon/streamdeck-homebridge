module StreamDeck.SDK.Utils

open Fable.Core.JS
open Browser.Dom
open Browser.Types
open Browser.WebSocket

let getCloseReason (event:Browser.Types.CloseEvent) =
    match event.code with
    | 1000 -> "Normal Closure. The purpose for which the connection was established has been fulfilled."
    | 1001 -> "Going Away. An endpoint is 'going away', such as a server going down or a browser having navigated away from a page."
    | 1002 -> "Protocol error. An endpoint is terminating the connection due to a protocol error"
    | 1003 -> "Unsupported Data. An endpoint received a type of data it doesn\'t support."
    | 1004 -> "--Reserved--. The specific meaning might be defined in the future."
    | 1005 -> "No Status. No status code was actually present."
    | 1006 -> "Abnormal Closure. The connection was closed abnormally, e.g., without sending or receiving a Close control frame"
    | 1007 -> "Invalid frame payload data. The connection was closed, because the received data was not consistent with the type of the message (e.g., non-UTF-8 [http://tools.ietf.org/html/rfc3629])."
    | 1008 -> "Policy Violation. The connection was closed, because current message data 'violates its policy'. This reason is given either if there is no other suitable reason, or if there is a need to hide specific details about the policy."
    | 1009 -> "Message Too Big. Connection closed because the message is too big for it to process."
    | 1010 ->
        // Note that this status code is not used by the server, because it can fail the WebSocket handshake instead.
        "Mandatory Ext. Connection is terminated the connection because the server didn\'t negotiate one or more extensions in the WebSocket handshake. <br /> Mandatory extensions were: " + event.reason
    | 1011 -> "Internal Server Error. Connection closed because it encountered an unexpected condition that prevented it from fulfilling the request."
    | 1015 -> "TLS Handshake. The connection was closed due to a failure to perform a TLS handshake (e.g., the server certificate can\'t be verified)."
    | _ -> $"Unknown reason ({event.code})"

let createWebSocket inPort =
    let websocket = WebSocket.Create("ws://127.0.0.1:" + inPort)
    websocket.onerror <- fun event -> 
        console.warn($"WEBOCKET ERROR: {event}");
    websocket.onclose <- fun event ->
        let reason = getCloseReason event
        console.warn("[STREAMDECK]***** WEBOCKET CLOSED **** reason: " + reason)
    websocket

let sendJson (websocket:WebSocket) (o:obj) =
    JSON.stringify o
    |> websocket.send

let parseJson (o:obj) =
    JSON.parse(o :?> string)