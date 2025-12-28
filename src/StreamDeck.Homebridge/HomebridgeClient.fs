module StreamDeck.Homebridge.Client

open Fable.SimpleHttp
open Fable.SimpleJson

type AuthResult = {
    access_token: string
    expires_in: int
    token_type: string
}

type AccessoryDetails = {
    aid: int
    iid: int
    uuid: string
    ``type``: string
    humanType: string
    serviceName: string
    serviceCharacteristics: AccessoryServiceCharacteristic[]
    instance: AccessoryInstance
    uniqueId: string
}

and AccessoryServiceCharacteristic = {
    uuid: string
    ``type``: string
    description: string
    value: obj option
    format: string
    maxValue: int option
    minValue: int option
    minStep: int option
    canWrite: bool
}

and AccessoryInstance = {
    name: string
    username: string
    ipAddress: string
    port: int
    services: string[]
    connectionFailedCount: int
}

type RoomLayout = {
    name: string
    services: AccessoryInfo[]
}

and AccessoryInfo = {
    uniqueId: string
    aid: int
    iid: int
    uuid: string
    customName: string option
}

type ConfigEditorInfo = { bridge: BridgeInfo }

and BridgeInfo = {
    name: string
    username: string
    port: int
    pin: string
}

module private Api =
    let inline private sendWithAuth (auth: AuthResult) (req: HttpRequest) =
        req
        |> Http.headers [
            Headers.contentType "application/json"
            Headers.authorization $"{auth.token_type} {auth.access_token}"
        ]
        |> Http.send

    let authenticate(serverInfo: Domain.GlobalSettings) =
        async {
            let body =
                Json.serialize {|
                    username = serverInfo.UserName
                    password = serverInfo.Password
                |}

            let! response =
                Http.request $"{serverInfo.Host}/api/auth/login"
                |> Http.method POST
                |> Http.content(BodyContent.Text body)
                |> Http.header(Headers.contentType "application/json")
                |> Http.withTimeout 5_000
                |> Http.send

            return
                if response.statusCode = 201 then
                    Json.tryParseAs<AuthResult> response.responseText
                else
                    let msg = $"Cannot authenticate to the server.\nResponse:{Json.serialize response}"
                    Error msg
        }

    let getAccessories host (auth: AuthResult) =
        async {
            let! response =
                Http.request $"%s{host}/api/accessories"
                |> Http.withTimeout 20_000
                |> sendWithAuth auth

            return
                if response.statusCode = 200 then
                    Json.tryParseAs<AccessoryDetails[]> response.responseText
                else
                    Error $"Cannot get accessories list from {host}. {response.responseText}"
        }

    let getAccessoriesLayout host (auth: AuthResult) =
        async {
            let! response =
                Http.request $"%s{host}/api/accessories/layout"
                |> Http.withTimeout 20_000
                |> sendWithAuth auth

            return
                if response.statusCode = 200 then
                    Json.tryParseAs<RoomLayout[]> response.responseText
                else
                    Error $"Cannot get room layout from {host}. {response.responseText}"
        }

    let setAccessoryCharacteristic
        host
        (auth: AuthResult)
        (uniqueId: string)
        (characteristicType: string)
        (value: obj)
        =
        async {
            let body =
                Json.serialize {|
                    characteristicType = characteristicType
                    value = value
                |}

            let! response =
                Http.request $"%s{host}/api/accessories/{uniqueId}"
                |> Http.method PUT
                |> Http.content(BodyContent.Text body)
                |> Http.withTimeout 5_000
                |> sendWithAuth auth

            return
                if response.statusCode = 200 then
                    Json.tryParseAs<AccessoryDetails> response.responseText
                else
                    Error
                        $"Cannot set accessory '{uniqueId}' characteristic '{characteristicType}' to '{value}'. {response.responseText}"
        }

    let getAccessory host (auth: AuthResult) (uniqueId: string) =
        async {
            let! response =
                Http.request $"%s{host}/api/accessories/{uniqueId}"
                |> sendWithAuth auth

            return
                if response.statusCode = 200 then
                    Json.tryParseAs<AccessoryDetails> response.responseText
                else
                    Error $"Cannot get accessory by id '{uniqueId}'. {response.responseText}"
        }


type HomebridgeClient(settings: Domain.GlobalSettings) =
    let mutable expiresAt = System.DateTime.Now
    let mutable authResult = None

    let getAuth() =
        async {
            let now = System.DateTime.Now

            match authResult with
            | Some res when now < expiresAt -> return Ok res
            | _ ->
                let! resp = Api.authenticate settings

                match resp with
                | Ok authResult' ->
                    authResult <- Some authResult'
                    let lifetime = System.TimeSpan.FromSeconds(float(authResult'.expires_in - 10 * 60))
                    expiresAt <- now + lifetime

                    return Ok authResult'
                | Error err -> return Error err
        }

    member _.Host = settings.Host

    member _.TestAuth() =
        async {
            match! getAuth() with
            | Ok _ -> return Ok()
            | Error err -> return Error err
        }

    member _.GetAccessories() =
        async {
            match! getAuth() with
            | Ok auth -> return! Api.getAccessories settings.Host auth
            | Error err -> return Error err
        }

    member _.GetAccessoriesLayout() =
        async {
            match! getAuth() with
            | Ok auth -> return! Api.getAccessoriesLayout settings.Host auth
            | Error err -> return Error err
        }

    member _.SetAccessoryCharacteristic (uniqueId: string) (characteristicType: string) (value: obj) =
        async {
            match! getAuth() with
            | Ok auth -> return! Api.setAccessoryCharacteristic settings.Host auth uniqueId characteristicType value
            | Error err -> return Error err
        }

    member _.GetAccessory(uniqueId: string) =
        async {
            match! getAuth() with
            | Ok auth -> return! Api.getAccessory settings.Host auth uniqueId
            | Error err -> return Error err
        }
