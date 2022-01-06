module StreamDeck.Homebridge.App

open Fable.Core.JS
open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.Plugin
open StreamDeck.SDK.PropertyInspector

open Elmish
open Elmish.Navigation
open Elmish.HMR
open Elmish.Debug


let createPluginAgent() :MailboxProcessor<PluginIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec idle() = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)
            match msg with
            | PluginIn_Connected(startArgs, replyAgent) ->
                return! loop startArgs replyAgent
            | _ -> return! idle()
        }
        and loop startArgs replyAgent = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)
            match msg with
            | PluginIn_KeyUp(event, payload) ->
                replyAgent.Post <| PluginOut_OpenUrl "https://www.elgato.com/en"
            | _ -> ()
            return! loop startArgs replyAgent
        }
        idle()
    )

let createPiAgent (dispatch: PI.PiMsg -> unit) :MailboxProcessor<PiIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec loop() = async{
            let! msg = inbox.Receive()
            console.log($"PI message is: %A{msg}", msg)

            match msg with
            | PiIn_Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PiOut_GetGlobalSettings
                dispatch <| PI.PiConnected(startArgs, replyAgent)
            | PiIn_DidReceiveSettings(event, payload) ->
                Domain.tryParse<Domain.ToggleSetting>(payload.settings)
                |> Option.iter (PI.ActionSettingReceived >> dispatch)
            | PiIn_DidReceiveGlobalSettings (settings) ->
                Domain.tryParse<Domain.GlobalSettings>(settings)
                |> Option.iter (PI.GlobalSettingsReceived >> dispatch)
            | PiIn_SendToPropertyInspector _ -> 
                ()

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
        let agent = createPluginAgent()
        connectPlugin args agent
    | "registerPropertyInspector" ->
        let subcribe model =
            let sub (dispatch: PI.PiMsg -> unit) =
                let agent = createPiAgent dispatch
                connectPropertyInspector args agent
            Cmd.ofSub sub

        Program.mkProgram PI.init PI.update PI.view
        |> Program.withSubscription subcribe
        |> Program.withReactBatched "elmish-app"
        |> Program.run
    | _ -> 
        console.error($"Unknown message type: %s{inMessageType} (connectElgatoStreamDeckSocket)")


let startPropertyInspectorApp() =
    Program.mkProgram PI.init PI.update PI.view
    #if DEBUG
    |> Program.withDebugger
    #endif
    |> Program.withReactBatched "elmish-app"
    |> Program.run
