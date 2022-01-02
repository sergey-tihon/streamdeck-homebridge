module StreamDeck.Homebridge.Client

open Browser
open Fable.SimpleHttp
open Fable.SimpleJson

let server = "http://192.168.0.1:8581"

type AuthInfo =
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
    }


let inline private sendWithAuth (auth:AuthInfo) (req:HttpRequest) =
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


let authenticate (username:string) (password:string) = 
    async {
        let body = Json.serialize {|
            username = username
            password = password
        |}
        let! responce = 
            Http.request $"{server}/api/auth/login" 
            |> Http.method POST
            |> Http.content (BodyContent.Text body)
            |> Http.header (Headers.contentType "application/json")
            |> Http.send
        return parseResp<AuthInfo> 201 responce
    }

let getAccessories (auth:AuthInfo) = 
    async {
        let! responce = 
            Http.request $"{server}/api/accessories"
            |> sendWithAuth auth
        return parseResp<AccessoryDetails[]> 200 responce
    }

let getAccessoriesLayout (auth:AuthInfo) =
    async {
        let! responce = 
            Http.request $"{server}/api/accessories/layout"
            |> sendWithAuth auth
        return parseResp<RoomLayout[]> 200 responce
    }

let setAccessoryCharacteristic (auth:AuthInfo) (uniqueId:string) (characteristicType:string) (value:int) = 
    async {
        let body = Json.serialize {|
            characteristicType = characteristicType
            value = value
        |}
        let! responce = 
            Http.request $"{server}/api/accessories/{uniqueId}"
            |> Http.method PUT
            |> Http.content (BodyContent.Text body)
            |> sendWithAuth auth
        return parseResp<AccessoryDetails> 200 responce
    }