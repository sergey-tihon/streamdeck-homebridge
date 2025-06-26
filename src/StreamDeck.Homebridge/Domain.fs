module StreamDeck.Homebridge.Domain

open Fable.SimpleJson

type GlobalSettings = {
    Host: string
    UserName: string
    Password: string
    UpdateInterval: int
}

type ActionSetting = {
    AccessoryId: string option
    CharacteristicType: string option
    TargetValue: float option
    Speed: int option
}

module ActionName =
    [<Literal>]
    let ConfigUi = "com.sergeytihon.homebridge.config-ui"

    [<Literal>]
    let Switch = "com.sergeytihon.homebridge.switch"

    [<Literal>]
    let Set = "com.sergeytihon.homebridge.set"

    [<Literal>]
    let Adjust = "com.sergeytihon.homebridge.adjust"

let inline tryParse<'t>(setting: obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind(fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | Error _ -> None)
