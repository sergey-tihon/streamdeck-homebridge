#r "nuget: Fun.Build, 1.0.5"

open Fun.Build
open System.IO

let options = {|
    GithubAction = EnvArg.Create("GITHUB_ACTION", description = "Run only in in github action container")
|}

let stage_update_manifest =
    stage "Update Manifest" {
        run(fun _ ->
            let version =
                Changelog.GetLastVersion(__SOURCE_DIRECTORY__)
                |> Option.defaultWith(fun () -> failwith "Version is not found")

            let fileName =
                Path.Combine(__SOURCE_DIRECTORY__, "src/com.sergeytihon.homebridge.sdPlugin/manifest.json")

            let lines =
                File.ReadAllLines fileName
                |> Seq.map(fun line ->
                    if line.TrimStart().StartsWith($"\"Version\":") then
                        let indent = line.Substring(0, line.IndexOf("\""))
                        sprintf "%s\"%s\": \"%s\"," indent "Version" version.Version
                    else
                        line)

            File.WriteAllLines(fileName, lines))
    }

pipeline "build" {
    description "Format code, build and package plugin"

    runBeforeEachStage(fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn $"::group::{ctx.Name}")

    runAfterEachStage(fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn "::endgroup::")

    stage "Check environment" {
        run "dotnet tool restore"
        run "dotnet paket restore"
        run "npm install"
        run(fun ctx -> printfn $"""github action name: {ctx.GetEnvVar options.GithubAction.Name}""")
    }

    stage "Lint" {
        stage "Format" {
            whenNot { envVar options.GithubAction }
            run "dotnet fantomas . -r"
        }

        stage "Check" {
            whenEnvVar options.GithubAction
            run "dotnet fantomas . -r --check"
        }
    }

    stage "Build" {
        run "rm -rf ./bin"
        run "mkdir bin"
        stage_update_manifest
        run "cp -r ./src/com.sergeytihon.homebridge.sdPlugin ./bin/com.sergeytihon.homebridge.sdPlugin"
        run "npm run build"
        run "./DistributionTool -b -i bin/com.sergeytihon.homebridge.sdPlugin -o ./bin"
    }

    runIfOnlySpecified
}


tryPrintPipelineCommandHelp()
