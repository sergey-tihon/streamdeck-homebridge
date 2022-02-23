module StreamDeck.Homebridge.Client

open Browser
open Fable.SimpleHttp
open Fable.SimpleJson

type AuthResult =
    {
        access_token: string
        expires_in: int
        token_type: string
    }

type AccessoryDetails = 
    {
        aid: int
        iid: int
        uuid: string
        ``type``: string
        humanType: string
        serviceName: string
        serviceCharacteristics: AccessoryServiceCharacteristic[]
        //accessoryInformation: AccessoryInformation
        //values: Map<string, obj>
        instance: AccessoryInstance
        uniqueId: string
    }
and AccessoryServiceCharacteristic =
    {
        //aid: int
        //iid: int
        uuid: string
        ``type``: string
        //serviceType: string
        //serviceName: string
        description: string
        value: obj option
        format: string
        //perms: string[]
        //unit: string option
        maxValue: int option
        minValue: int option
        minStep: int option
        //canRead: bool
        canWrite: bool
        //ev: bool
    }
// and AccessoryInformation = 
//     {
//         Manufacturer: string
//         Model: string
//         Name: string option
//     }
and AccessoryInstance = {
    name: string
    username: string
    ipAddress: string
    port:int
    services: string[]
    connectionFailedCount: int
}

type RoomLayout = 
    {
        name: string
        services: AccessoryInfo[]
    }
and AccessoryInfo = 
    {
        uniqueId: string
        aid: int
        iid: int
        uuid: string
        customName: string option
    }

type ConfigEditorInfo = {
    bridge: BridgeInfo
}
and BridgeInfo = {
    name: string
    username: string
    port: int
    pin: string
}

let inline private sendWithAuth (auth:AuthResult) (req:HttpRequest) =
    req
    |> Http.headers [
        Headers.contentType "application/json"
        Headers.authorization $"{auth.token_type} {auth.access_token}"
    ]
    |> Http.send

let authenticate (serverInfo:Domain.GlobalSettings) = 
    async {
        let body = Json.serialize {|
            username = serverInfo.UserName
            password = serverInfo.Password
        |}
        let! responce = 
            Http.request $"{serverInfo.Host}/api/auth/login" 
            |> Http.method POST
            |> Http.content (BodyContent.Text body)
            |> Http.header (Headers.contentType "application/json")
            |> Http.withTimeout 5_000
            |> Http.send
        return 
            if responce.statusCode = 201 then
                Json.tryParseAs<AuthResult> responce.responseText
            else Error ($"Unsuccessful login: Server is unavailable or login/password is incorrect. {responce.responseText}")
    }

let getConfigEditorInfo host (auth:AuthResult) = 
    async {
        let! responce = 
            Http.request $"%s{host}/api/config-editor"
            |> sendWithAuth auth
        return 
            if responce.statusCode = 200 then
                Json.tryParseAs<ConfigEditorInfo> responce.responseText
            else Error ($"Cannot get config editor info from {host}. {responce.responseText}")
    }

let getAccessories host (auth:AuthResult) = 
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories"
            |> Http.withTimeout 20_000
            |> sendWithAuth auth
        return 
            if responce.statusCode = 200 then
                Json.tryParseAs<AccessoryDetails[]> responce.responseText
            else Error ($"Cannot get accessories list from {host}. {responce.responseText}")
    }

let getAccessoriesLayout host (auth:AuthResult) =
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories/layout"
            |> Http.withTimeout 20_000
            |> sendWithAuth auth
        return 
            if responce.statusCode = 200 then
                Json.tryParseAs<RoomLayout[]> responce.responseText
            else Error ($"Cannot get room layout from {host}. {responce.responseText}")
    }

let setAccessoryCharacteristic host (auth:AuthResult) (uniqueId:string) (characteristicType:string) (value:obj) = 
    async {
        let body = Json.serialize {|
            characteristicType = characteristicType
            value = value
        |}
        let! responce = 
            Http.request $"%s{host}/api/accessories/{uniqueId}"
            |> Http.method PUT
            |> Http.content (BodyContent.Text body)
            |> sendWithAuth auth
        return 
            if responce.statusCode = 200 then
                Json.tryParseAs<AccessoryDetails> responce.responseText
            else Error ($"Cannot set accessory '{uniqueId}' characteristic '{characteristicType}' to '{value}'. {responce.responseText}")
    }

let getAccessory host (auth:AuthResult) (uniqueId:string)= 
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories/{uniqueId}"
            |> sendWithAuth auth
        return 
            if responce.statusCode = 200 then
                Json.tryParseAs<AccessoryDetails> responce.responseText
            else Error ($"Cannot get accessory by id '{uniqueId}'. {responce.responseText}")
    }