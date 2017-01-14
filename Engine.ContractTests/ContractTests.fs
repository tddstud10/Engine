module R4nd0mApps.TddStud10.Engine.Core.ContractTests

open ApprovalTests
open ApprovalTests.Namers
open ApprovalTests.Reporters
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System
open System.IO
open Xunit
open ContractTestHelpers

let solutions = [ @"CSXUnit1xNUnit3x.NET20\CSXUnit1xNUnit3x.sln"
                  //@"FSXUnit2xNUnit2x.NET45\FSXUnit2xNUnit2x.sln"
                  @"VBXUnit1xNUnit2x.NET40\VBXUnit1xNUnit2x.sln" ]

let variants = [ "BREAK_NOTHING"
                 "BREAK_TEST"
                 "BREAK_BUILD" ]

let ``Test Data - E2E Run for Project`` : obj array seq = 
    seq { 
        for s in solutions do
            for v in variants -> s, v
    }
    |> Seq.map (fun (a, b) -> 
           [| box a
              box b |])

let runEngine sln props = 
    let ssr = sprintf @"%s\%O" binRoot (Guid.NewGuid())
    let h = TddStud10HostProxy(9999, false) :> ITddStud10Host
    try 
        async { 
            let ds = h.GetDataStore()
            let eEvents = collectEngineEvents <| h.GetEngineEvents()
            let dsEvents = collectDataStoreEvents <| h.GetDataStoreEvents()
            let! ver = h.Start()
            Assert.Equal(ver, App.getVersion())
            let e = h.GetEngine()
            let! enabled = e.IsEnabled()
            Assert.False(enabled)
            let! ret = e.EnableEngine()
            Assert.True(ret)
            let! enabled = e.IsEnabled()
            Assert.True(enabled)
            let! ret = e.DisableEngine()
            Assert.False(ret)
            let! enabled = e.IsEnabled()
            Assert.False(enabled)
            let! _ = e.EnableEngine()
            let ep = 
                { HostVersion = HostVersion.VS2015
                  EngineConfig = EngineConfig(SnapShotRoot = ssr, AdditionalMSBuildProperties = props)
                  SolutionPath = getTestProjectsRoot sln |> FilePath
                  SessionStartTime = DateTime.UtcNow.AddMinutes(-1.0) }
            let! runInProgress = e.RunEngine ep
            Assert.True(runInProgress)
            do! waitWhileRunInProgress e
            let! dsState = ds.GetSerializedState()
            let runOutput = 
                sprintf "[%s\r\n,%s]" ([ eEvents.ToArray()
                                         dsEvents.ToArray() ]
                                       |> toJson) dsState
            h.Stop()
            Assert.False(h.IsRunning)
            return runOutput, ep.SolutionPath
        }
        |> Async.RunSynchronously
    finally
        h.Stop()
        if Directory.Exists ssr then Exec.safeExec (fun () -> Directory.Delete(ssr, true))

[<UseReporter(typeof<DiffReporter>)>]
[<UseApprovalSubdirectory("approvals")>]
[<Theory>]
[<MemberData("Test Data - E2E Run for Project")>]
let ``E2E Run for Project`` (s : string, v : string) = 
    use __ = ApprovalResults.ForScenario(Path.GetDirectoryName(s), v)
    let output, projRoot = runEngine s [| sprintf "DefineConstants=%s" v |]
    Approvals.Verify
        (output, Func<_, _>(normalizeJsonDoc binRoot (Path.GetDirectoryName(projRoot.ToString()))))
(*
o Test enhancement
  - datastore api tests
o Update 
  - FSharp.Core to 4.0
  - XUnit and add FSUnit and FSCheck
o Additional enhancements
  - Logging
  - Telemetry
o Add other stuff in YoLo 
  - from prelude esp
  - let inline ofNull value
- Telemetry.Flush on closing solution hangs VS
*)
