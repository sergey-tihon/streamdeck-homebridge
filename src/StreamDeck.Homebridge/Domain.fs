module StreamDeck.Homebridge.Domain

type GlobalSettings = 
    {
        Host: string
        UserName: string
        Password: string
    }

type ToggleSetting =
    {
        AccessoryId: string option
        CharacteristicType: string option
    }

open Fable.SimpleJson
let inline tryParse<'t> (setting:obj) : 't option =
    SimpleJson.fromObjectLiteral setting
    |> Option.bind (fun json ->
        match Json.tryConvertFromJsonAs json with
        | Ok x -> Some x
        | _ -> None
    )