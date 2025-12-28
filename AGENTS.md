# AGENTS.md - Coding Agent Guidelines

Stream Deck plugin for controlling Homebridge accessories. Built with F# compiled to JavaScript via Fable, using Elmish architecture.

**Tech Stack:** F# + Fable, Feliz/React, Elmish (MVU), Webpack, Stream Deck SDK v2

## Build Commands

```bash
./build.sh                    # Full pipeline (restore, lint, validate, build, package)
npm start                     # Dev mode with hot reload (port 4200)
npm run build                 # Production build (Fable + Webpack)
npm install                   # Install dependencies (npm + dotnet tools)

dotnet tool restore           # Restore .NET CLI tools
dotnet paket restore          # Restore NuGet packages
dotnet fable src/StreamDeck.Homebridge  # Compile F# to JS
npx webpack                   # Bundle
npx @elgato/cli validate ./src/com.sergeytihon.homebridge.sdPlugin/
dotnet fsi build.fsx -- -p build  # Run build pipeline
```

## Testing

**Note:** No test suite configured. No test files or test framework dependencies.

## Linting and Formatting

```bash
dotnet fantomas .             # Format all F# files
dotnet fantomas . --check     # Check formatting (CI mode)
```

**EditorConfig rules:**
- Indent: 4 spaces
- `fsharp_multiline_bracket_style = stroustrup`
- `fsharp_bar_before_discriminated_union_declaration = true`
- `fsharp_space_before_parameter = false`
- `fsharp_space_before_lowercase_invocation = false`

## Code Style Guidelines

### Module Declaration
```fsharp
module StreamDeck.Homebridge.Domain
```

### Import Organization
Order: System -> Third-party -> Project-specific
```fsharp
open System
open Fable.SimpleHttp
open StreamDeck.SDK.Dto
```

### Record Types (Stroustrup Style)
```fsharp
type GlobalSettings = {
    Host: string
    UserName: string
    Password: string
}
```

### Discriminated Unions
Always use bar before first case:
```fsharp
[<RequireQualifiedAccess>]
type PiMsg =
    | PiConnected of startArgs: StartArgs
    | GlobalSettingsReceived of Domain.GlobalSettings
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Types | PascalCase | `GlobalSettings`, `PiModel` |
| Functions | camelCase | `processKeyUp`, `updateAccessories` |
| Parameters | camelCase | `serverInfo`, `uniqueId` |
| Modules | PascalCase | `ActionName`, `Api` |
| Literals | PascalCase in modules | `ActionName.Switch` |
| Private modules | `module private Name` | `module private Api =` |

### Constants/Literals
```fsharp
module ActionName =
    [<Literal>]
    let ConfigUi = "com.sergeytihon.homebridge.config-ui"
    
    [<Literal>]
    let Switch = "com.sergeytihon.homebridge.switch"
```

### Error Handling
Use `Result<'T, string>` pattern:
```fsharp
match! client.GetAccessories() with
| Ok accessories -> // handle success
| Error err -> // handle error

async {
    match! getAuth() with
    | Ok auth -> return! Api.getAccessories host auth
    | Error err -> return Error err
}
```

### Async Patterns
```fsharp
async {
    let! response = Http.request url |> Http.send
    return
        if response.statusCode = 200 then
            Json.tryParseAs<MyType> response.responseText
        else
            Error $"Request failed: {response.responseText}"
}
```

### MailboxProcessor (Agent) Pattern
```fsharp
MailboxProcessor.Start(fun inbox ->
    let rec loop() =
        async {
            let! msg = inbox.Receive()
            // handle message
            return! loop()
        }
    loop())
```

### Feliz/React Components
```fsharp
Html.div [
    prop.className "sdpi-item"
    prop.children [
        Html.div [ prop.className "sdpi-item-label"; prop.text "Label" ]
        Html.button [
            prop.onClick (fun _ -> dispatch Msg.Click)
            prop.text "Click me"
        ]
    ]
]
```

## Project Structure

```
src/
├── StreamDeck.SDK/           # Reusable SDK (Dto.fs, Utils.fs, PluginModel.fs)
├── StreamDeck.Homebridge/    # Main app (Domain.fs, HomebridgeClient.fs, App.fs)
│   ├── Agents/               # MailboxProcessor agents
│   └── Pi/                   # Property Inspector UI (Model/Update/View)
└── com.sergeytihon.homebridge.sdPlugin/  # Plugin resources & manifest
```

## SDK Requirements

- .NET SDK 10.0.100+ (see global.json)
- Node.js 22+
