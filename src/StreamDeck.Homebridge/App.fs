module StreamDeck.Homebridge.App

open Fable.Core.JS
open Browser.Dom
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PluginModel
open StreamDeck.SDK.PiModel

open Elmish
open Elmish.Navigation
open Elmish.HMR
open Elmish.Debug


let createPluginAgent() :MailboxProcessor<PluginIn_Events> = 
    MailboxProcessor.Start(fun inbox->
        let rec idle() = async {
            let! msg = inbox.Receive()
            match msg with
            | PluginIn_Connected(startArgs, replyAgent) ->
                replyAgent.Post <| PluginOut_GetGlobalSettings
                return! loop startArgs replyAgent None
            | _ -> 
                console.warn($"Idle plugin agent received unexpected message %A{msg}", msg)
                return! idle()
        }
        and loop startArgs replyAgent globalSetting = async {
            let! msg = inbox.Receive()
            console.log($"Plugin message is: %A{msg}", msg)
            match msg with
            | PluginIn_DidReceiveGlobalSettings settings ->
                let settings =  Domain.tryParse<Domain.GlobalSettings>(settings)
                return! loop startArgs replyAgent settings
            | PluginIn_KeyUp(event, payload) ->
                let onError (message:string) = 
                    console.warn(message)
                    replyAgent.Post <| PluginOut_LogMessage message
                    replyAgent.Post <| PluginOut_ShowAlert event.context

                let actionSettings = Domain.tryParse<Domain.SwitchSetting>(payload.settings)
                match globalSetting, actionSettings with
                | Some(serverInfo), Some({ AccessoryId = Some(accessoryId); CharacteristicType = Some(characteristicType)}) ->
                    match! Client.authenticate serverInfo with
                    | Ok authInfo ->
                        let! accessory = Client.getAccessory serverInfo.Host authInfo accessoryId
                        match accessory with
                        | Some(accessory) ->
                            let currentValue = accessory |> PI.getIntValue characteristicType
                            let targetValue = 1 - currentValue
                            let! accessory' = Client.setAccessoryCharacteristic serverInfo.Host authInfo accessoryId characteristicType targetValue
                            match accessory' with
                            | Some(accessory') ->
                                let currentValue = accessory' |> PI.getIntValue characteristicType
                                replyAgent.Post <| PluginOut_SetState (event.context, currentValue) 
                            | None -> onError $"Cannot toggle characteristic '{characteristicType}' of accessory '{accessoryId}'"
                        | None -> onError $"Cannot find accessory by id '{accessoryId}'"
                    | Error e -> onError $"Authentication issue: ${e}"
                | _ ->  onError "Action is not properly configured"

                return! loop startArgs replyAgent globalSetting
            | _ ->
                return! loop startArgs replyAgent globalSetting
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
                Domain.tryParse<Domain.SwitchSetting>(payload.settings)
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

        Program.mkProgram (PI.init false) PI.update PI.view
        |> Program.withSubscription subcribe
        |> Program.withReactBatched "elmish-app"
        |> Program.run
    | _ -> 
        console.error($"Unknown message type: %s{inMessageType} (connectElgatoStreamDeckSocket)")


let startPropertyInspectorApp() =
    Program.mkProgram (PI.init true) PI.update PI.view
    #if DEBUG
    |> Program.withDebugger
    #endif
    |> Program.withReactBatched "elmish-app"
    |> Program.run
