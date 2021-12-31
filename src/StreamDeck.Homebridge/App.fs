module StreamDeck.Homebridge.App

open Fable.Core.JS
open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.Plugin
open StreamDeck.SDK.PropertyInspector


let plugin :MailboxProcessor<PluginIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec idle() = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}")
            match msg with
            | PluginIn_Connected(startArgs, replyAgent) ->
                return! loop startArgs replyAgent
            | _ -> return! idle()
        }
        and loop startArgs replyAgent = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}")
            match msg with
            | PluginIn_KeyUp(event, payload) ->
                replyAgent.Post <| PluginOut_OpenUrl "https://www.elgato.com/en"
            | _ -> ()
            return! loop startArgs replyAgent
        }
        idle()
    )

let pi :MailboxProcessor<PiIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec loop() = async{
            let! msg = inbox.Receive()
            console.log($"PI message is: %A{msg}")
            return! loop()
        }
        loop()
    )


/// <summary> connectElgatoStreamDeckSocket
/// This is the first function StreamDeck Software calls, when
/// establishing the connection to the plugin or the Property Inspector </summary>
/// <param name="inPort">The socket's port to communicate with StreamDeck software.</param>
/// <param name="inUUID">A unique identifier, which StreamDeck uses to communicate with the plugin.</param>
/// <param name="inMessageType">Identifies, if the event is meant for the property inspector or the plugin.</param>
/// <param name="inApplicationInfo">Information about the host (StreamDeck) application.</param>
/// <param name="inActionInfo">Context is an internal identifier used to communicate to the host application.</param>
let connectElgatoStreamDeckSocket (inPort:string, inUUID:string, inMessageType:string, inApplicationInfo:string, inActionInfo:string) =
    let args : StartArgs = {
        Port = inPort
        UUID = inUUID
        MessageType = inMessageType
        ApplicationInfo = JSON.parse(inApplicationInfo) :?> ApplicationInfo
        ActionInfo = 
            if isNull inActionInfo then None
            else JSON.parse(inActionInfo) :?> ActionInfo |> Some
    }
    match inMessageType with
    | "registerPlugin" ->
        connectPlugin args plugin
    | "registerPropertyInspector" ->
        connectPropertyInspector args pi
    | _ -> 
        console.error($"Unknown message type: %s{inMessageType} (connectElgatoStreamDeckSocket)")
