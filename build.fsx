#r @"paket:
source https://nuget.org/api/v2
framework netstandard2.0
nuget FSharp.Core 5.0.0
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes 
nuget Fake.DotNet.Paket
nuget Fake.DotNet.Cli //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif


// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

open Fake
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open System
open System.IO

let bin = "bin"
let name = "com.sergeytihon.homebridge.sdPlugin"

let releaseNotesData =
    File.ReadAllLines "RELEASE_NOTES.md"
    |> ReleaseNotes.parseAll
let release = List.head releaseNotesData    

let macTarget = 
    IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "/Library/Application Support/com.elgato.StreamDeck/Plugins"
    )

// Targets
Target.create "Clean" (fun _ ->
    Shell.mkdir bin
    Shell.cleanDir bin
)

Target.create "NpmInstall" (fun _ ->
    Shell.Exec("npm", "install") 
    |>  function
        | 0 -> ()
        | code -> failwithf "build failed with code %d" code
)

let setManifestJsonField fieldName value =
    let fileName = Path.Combine( __SOURCE_DIRECTORY__, "src", name, "manifest.json")

    let lines =
        File.ReadAllLines fileName
        |> Seq.map (fun line ->
            if line.TrimStart().StartsWith($"\"{fieldName}\":") then
                let indent = line.Substring(0, line.IndexOf("\""))
                sprintf "%s\"%s\": %s," indent fieldName value
            else
                line)

    File.WriteAllLines(fileName, lines)

Target.create "Build" (fun _ ->
    let versionString = $"\"{release.NugetVersion}\""
    setManifestJsonField "Version" versionString

    Shell.copyDir $"bin/{name}" $"src/{name}" (fun s -> not <| s.Contains("/js/"))
    Shell.Exec("npm", "run build") 
    |>  function
        | 0 -> ()
        | code -> failwithf "build failed with code %d" code
)

Target.create "Release" (fun _ ->
    // DistributionTool -b -i com.elgato.counter.sdPlugin -o ~/Desktop/
    CreateProcess.fromRawCommand 
        "./paket-files/developer.elgato.com/DistributionTool"
        ["-b"; "-i"; $"bin/{name}"; "-o"; "./bin"]
    |> Proc.run 
    |> fun res ->
        if res.ExitCode <> 0
        then failwithf "DistributionTool failed with code %d" res.ExitCode
)

Target.create "Deploy" (fun _ ->
    let target = $"{macTarget}/{name}"
    Shell.deleteDir target
    Shell.copyDir target $"bin/{name}" (fun _ -> true)
    Process.killAllByName "Stream Deck"

    // CreateProcess.fromRawCommand 
    //     "/Applications/Stream Deck.app/Contents/MacOS/Stream Deck" []
    // |> Proc.start |> ignore
)

Target.create "All" ignore

// Build order
"Clean"
    ==> "NpmInstall"
    ==> "Build"
    ==> "Release"
    ==> "All"
    ==> "Deploy"

// start build
Target.runOrDefault "All"