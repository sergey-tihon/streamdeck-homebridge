module StreamDeck.Homebridge.Domain

open Fable.SimpleJson
open StreamDeck.SDK

type GlobalSettings = {
    Host: string
    UserName: string
    Password: string
}

type ActionSetting = {
    AccessoryId: string option
    CharacteristicType: string option
    TargetValue: float option
}

[<Literal>]
let CONFIG_ACTION_NAME = "com.sergeytihon.homebridge.config-ui"

[<Literal>]
let SWITCH_ACTION_NAME = "com.sergeytihon.homebridge.switch"

[<Literal>]
let SET_ACTION_NAME = "com.sergeytihon.homebridge.set"

let inline tryParse<'t>(setting: obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind(fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | Error e -> None)
