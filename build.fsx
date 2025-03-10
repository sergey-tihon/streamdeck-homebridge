#r "nuget: Fun.Build, 1.1.16"

open Fun.Build
open System.IO

let options = {|
    GithubAction = EnvArg.Create("GITHUB_ACTION", description = "Run only in in github action container")
|}

let version =
    Changelog.GetLastVersion(__SOURCE_DIRECTORY__)
    |> Option.defaultWith(fun () -> failwith "Version is not found")


pipeline "build" {
    workingDir __SOURCE_DIRECTORY__

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
            run "dotnet fantomas ."
        }

        stage "Check" {
            whenEnvVar options.GithubAction
            run "dotnet fantomas . --check"
        }
    }

    stage "Validate" { run "npx @elgato/cli validate ./src/com.sergeytihon.homebridge.sdPlugin/" }

    stage $"Build %s{version.Version}" {
        run "rm -rf ./bin"
        run "mkdir bin"
        run "cp -r ./src/com.sergeytihon.homebridge.sdPlugin ./bin/com.sergeytihon.homebridge.sdPlugin"
        run "npm run build"

        run
            $"npx @elgato/cli pack bin/com.sergeytihon.homebridge.sdPlugin --output bin/ --version %s{version.Version} -f"
    }

    runIfOnlySpecified
}

tryPrintPipelineCommandHelp()
