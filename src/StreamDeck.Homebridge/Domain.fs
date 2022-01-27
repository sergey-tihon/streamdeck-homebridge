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
    }

let CONFIG_ACTION_NAME = "com.sergeytihon.homebridge.config-ui"
let SWITCH_ACTION_NAME = "com.sergeytihon.homebridge.switch"

let (|ConfigAction|_|) (action:string) =
    if action = CONFIG_ACTION_NAME then Some() else None

let (|SwitchAction|_|) (action:string) =
    if action = SWITCH_ACTION_NAME then Some() else None

let inline tryParse<'t> (setting:obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind (fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | _ -> None
    )