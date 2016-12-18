// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.IO

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some MSBuildVerbosity.Quiet }

// Directories
let buildDir  = "./build/"

// Targets
Target "Build" (fun _ ->
    !! "*.*proj"
    |> MSBuildRelease buildDir "Build" 
    |> ignore
)

// Start build
RunTargetOrDefault "Build"
