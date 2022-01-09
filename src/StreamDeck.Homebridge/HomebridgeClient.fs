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
        accessoryInformation: AccessoryInformation
        //values: Map<string, obj>
        instance: AccessoryInstance
        uniqueId: string
    }
and AccessoryServiceCharacteristic =
    {
        aid: int
        iid: int
        uuid: string
        ``type``: string
        serviceType: string
        serviceName: string
        description: string
        value: obj
        format: string
        perms: string[]
        unit: string option
        maxValue: int option
        minValue: int option
        minStep: int option
        canRead: bool
        canWrite: bool
        ev: bool
    }
and AccessoryInformation = 
    {
        Manufacturer: string
        Model: string
        Name: string
        //``Serial Number``: string
        //``Firmware Revision``: string
        //``Hardware Revision``: string
    }
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

let inline private parseResp<'a> successCode (responce:HttpResponse) =
    if responce.statusCode = successCode then
        let resp = Json.tryParseAs<'a> responce.responseText
        match resp with
        | Ok data -> Some (data)
        | _ -> None
    else None


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
            |> Http.send
        return 
            if responce.statusCode = 201 then
                Json.tryParseAs<AuthResult> responce.responseText
            else Error ($"Unsuccessful login. {responce.responseText}")
    }

let getConfigEditorInfo host (auth:AuthResult) = 
    async {
        let! responce = 
            Http.request $"%s{host}/api/config-editor"
            |> sendWithAuth auth
        return parseResp<ConfigEditorInfo> 200 responce
    }

let getAccessories host (auth:AuthResult) = 
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories"
            |> sendWithAuth auth
        return parseResp<AccessoryDetails[]> 200 responce
    }

let getAccessoriesLayout host (auth:AuthResult) =
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories/layout"
            |> sendWithAuth auth
        return parseResp<RoomLayout[]> 200 responce
    }

let setAccessoryCharacteristic host (auth:AuthResult) (uniqueId:string) (characteristicType:string) (value:int) = 
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
        return parseResp<AccessoryDetails> 200 responce
    }

let getAccessory host (auth:AuthResult) (uniqueId:string)= 
    async {
        let! responce = 
            Http.request $"%s{host}/api/accessories/{uniqueId}"
            |> sendWithAuth auth
        return parseResp<AccessoryDetails> 200 responce
    }