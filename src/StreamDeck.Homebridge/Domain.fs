module StreamDeck.Homebridge.Domain

open Fable.SimpleJson
open StreamDeck.SDK

type GlobalSettings = 
    {
        Host: string
        UserName: string
        Password: string
    }

type ActionSetting =
    {
        AccessoryId: string option
        CharacteristicType: string option
        TargetValue: float option
    }

let [<Literal>]CONFIG_ACTION_NAME = "com.sergeytihon.homebridge.config-ui"
let [<Literal>]SWITCH_ACTION_NAME = "com.sergeytihon.homebridge.switch"
let [<Literal>]SET_ACTION_NAME    = "com.sergeytihon.homebridge.set"

let inline tryParse<'t> (setting:obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind (fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | Error e ->
            None
    )
