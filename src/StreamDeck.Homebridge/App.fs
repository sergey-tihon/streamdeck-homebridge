module StreamDeck.Homebridge.App

open Fable.Core.JS
open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PluginModel
open StreamDeck.SDK.PiModel

open Elmish
open Elmish.HMR
open Elmish.Debug


/// <summary> connectElgatoStreamDeckSocket
/// This is the first function StreamDeck Software calls, when
/// establishing the connection to the plugin or the Property Inspector </summary>
/// <param name="inPort">The socket's port to communicate with StreamDeck software.</param>
/// <param name="inUUID">A unique identifier, which StreamDeck uses to communicate with the plugin.</param>
/// <param name="inMessageType">Identifies, if the event is meant for the property inspector or the plugin.</param>
/// <param name="inApplicationInfo">Information about the host (StreamDeck) application.</param>
/// <param name="inActionInfo">Context is an internal identifier used to communicate to the host application.</param>
let connectElgatoStreamDeckSocket
    (
        inPort: string,
        inUUID: string,
        inMessageType: string,
        inApplicationInfo: string,
        inActionInfo: string
    ) =
    let args: StartArgs = {
        Port = inPort
        UUID = inUUID
        MessageType = inMessageType
        ApplicationInfo = JSON.parse(inApplicationInfo) :?> ApplicationInfo
        ActionInfo =
            if isNull inActionInfo then
                None
            else
                JSON.parse(inActionInfo) :?> ActionInfo |> Some
    }

    match inMessageType with
    | "registerPlugin" ->
        let agent = PluginAgent.createPluginAgent()
        connectPlugin args agent
    | "registerPropertyInspector" ->
        let subscribe model =
            let sub(dispatch: PiView.PiMsg -> unit) =
                let agent = PiAgent.createPiAgent dispatch
                connectPropertyInspector args agent

            Cmd.ofSub sub
        //     { new System.IDisposable with
        //         member _.Dispose() = ()
        //     }

        // [ [ "ws" ], sub ]


        Program.mkProgram (PiView.init false) PiView.update PiView.render
        |> Program.withSubscription subscribe
        |> Program.withReactBatched "elmish-app"
        |> Program.run
    | _ -> console.error($"Unknown message type: %s{inMessageType} (connectElgatoStreamDeckSocket)")


let startPropertyInspectorApp() =
    Program.mkProgram (PiView.init true) PiView.update PiView.render
#if DEBUG
    |> Program.withDebugger
#endif
    |> Program.withReactBatched "elmish-app"
    |> Program.run
