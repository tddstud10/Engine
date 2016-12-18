// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.IO

// Directories
let buildDir  = "./build/"

// Targets
Target "Build" (fun _ ->
    !! "*.*proj"
    |> MSBuildRelease buildDir "Build" 
    |> Log "Build-Output: "
)

// Start build
RunTargetOrDefault "Build"
