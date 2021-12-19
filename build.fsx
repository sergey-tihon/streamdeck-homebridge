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

let bin = "bin"
let name = "com.sergeytihon.homebridge.sdPlugin"
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

Target.create "Build" (fun _ ->
    Shell.copyDir $"bin/{name}" $"src/{name}" (fun _ -> true)
)

Target.create "Release" (fun _ ->
    // DistributionTool -b -i com.elgato.counter.sdPlugin -o ~/Desktop/
    CreateProcess.fromRawCommand 
        "./paket-files/developer.elgato.com/DistributionTool"
        ["-b"; "-i"; $"bin/{name}"; "-o"; "./bin"]
    |> Proc.run |> ignore
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
    ==> "Build"
    ==> "Release"
    ==> "All"
    ==> "Deploy"

// start build
Target.runOrDefault "All"