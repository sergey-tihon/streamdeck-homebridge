module StreamDeck.Homebridge.GTag

open Fable.Core

[<Emit("gtag('event', $0, $1)")>]
let private log (eventName: string) (eventParams: obj): unit = jsNative

/// gtag('event', 'exception', {
///   'description': 'error_description',
///   'fatal': false   // set to true if the error is fatal
/// });
let logException (description: string) =
    log "exception" {|
        description = description
        fatal = false
    |}
    
/// gtag('event', <action>, {
///   'event_category': <category>,
///   'event_label': <label>,
///   'value': <value>
/// });
let logEvent (action: string) (category: string) (label:string) (value:int) =
    log action {|
        event_category = category
        event_label = label
        value = value
    |}
