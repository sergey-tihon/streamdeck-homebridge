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

let (|ConfigAction|_|) (event:Dto.Event) =
    if event.action = "com.sergeytihon.homebridge.config-ui"
    then Some() else None

let (|SwitchAction|_|) (event:Dto.Event) =
    if event.action = "com.sergeytihon.homebridge.switch"
    then Some() else None

let inline tryParse<'t> (setting:obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind (fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | _ -> None
    )