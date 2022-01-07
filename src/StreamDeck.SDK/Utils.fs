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
    let data = JSON.stringify o
    websocket.send data

let parseJson (o:obj) =
    JSON.parse(o :?> string)

open Fable.Core
open Fable.Core.JS


[<Emit("'#' + $0.toString(16).padStart(6, 0)")>]
let private toHexColor (color: int): string = jsNative

[<Emit("parseInt($0, 16)")>]
let private fromHexColor (color:string): int = jsNative

/// Quick utility to lighten or darken a color (doesn't take color-drifting, etc. into account)
/// Usage:
///   fadeColor('#061261', 100); // will lighten the color
///   fadeColor('#200867'), -100); // will darken the color
let fadeColor (col:string, amt:int) =
    let norm x = min 255 (max x 0)
    //let num = parseInt (col.TrimStart('#')) 16
    let num = col.TrimStart('#') |> fromHexColor
    let r = norm((num >>> 16) + amt)
    let g = norm((num &&& 0x0000FF) + amt)
    let b = norm(((num >>> 8) &&& 0x00FF) + amt)
    toHexColor(g ||| (b <<< 8) ||| (r <<< 16)) 

let addDynamicStyles (clrs:Dto.ColorInfo) =
    console.warn(clrs)
    let mouseDownColor = 
        if isNull clrs.mouseDownColor
        then fadeColor(clrs.highlightColor,-100)
        else clrs.mouseDownColor

    let clr = clrs.highlightColor.Substring(0, 7)
    let clr1 = fadeColor(clr, 100)
    let clr2 = fadeColor(clr, 60)
    let metersActiveColor = fadeColor(clr, -60)

    //let mutable node = document.getElementById "#sdpi-dynamic-styles"
    let node = document.createElement "style"

    node.setAttribute("id", "sdpi-dynamic-styles")
    node.innerHTML <- $"""
    input[type="radio"]:checked + label span,
    input[type="checkbox"]:checked + label span {{
        background-color: {clrs.highlightColor};
    }}

    input[type="radio"]:active:checked + label span,
    input[type="checkbox"]:active:checked + label span {{
        background-color: {mouseDownColor};
    }}

    input[type="radio"]:active + label span,
    input[type="checkbox"]:active + label span {{
        background-color: {clrs.buttonPressedBorderColor};
    }}

    td.selected,
    td.selected:hover,
    li.selected:hover,
    li.selected {{
        color: white;
        background-color: {clrs.highlightColor};
    }}

    .sdpi-file-label > label:active,
    .sdpi-file-label.file:active,
    label.sdpi-file-label:active,
    label.sdpi-file-info:active,
    input[type="file"]::-webkit-file-upload-button:active,
    button:active {{
        border: 1pt solid {clrs.buttonPressedBorderColor};
        background-color: {clrs.buttonPressedBackgroundColor};
        color: {clrs.buttonPressedTextColor};
        border-color: {clrs.buttonPressedBorderColor};
    }}

    ::-webkit-progress-value,
    meter::-webkit-meter-optimum-value {{
        background: linear-gradient({clr2}, {clr1} 20%%, {clr} 45%%, {clr} 55%%, {clr2})
    }}

    ::-webkit-progress-value:active,
    meter::-webkit-meter-optimum-value:active {{
        background: linear-gradient({clr}, {clr2} 20%%, {metersActiveColor} 45%%, {metersActiveColor} 55%%, {clr})
    }}
    """

    node |> document.body.appendChild |> ignore
