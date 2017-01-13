namespace R4nd0mApps.TddStud10.Engine.Core

module ContractTests = 
    open ApprovalTests
    open ApprovalTests.Namers
    open ApprovalTests.Reporters
    open R4nd0mApps.TddStud10.Engine.Core
    open System
    open System.IO
    open Xunit
    
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
    
    [<UseReporter(typeof<DiffReporter>)>]
    [<UseApprovalSubdirectory("approvals")>]
    [<Theory>]
    [<MemberData("Test Data - E2E Run for Project")>]
    let ``E2E Run for Project`` (s : string, v : string) = 
        use __ = ApprovalResults.ForScenario(Path.GetDirectoryName(s), v)
        let output, projRoot = Helpers.runEngine s [| sprintf "DefineConstants=%s" v |]
        Approvals.Verify
            (output, Func<_, _>(Helpers.normalizeJsonDoc Helpers.binRoot (Path.GetDirectoryName(projRoot.ToString()))))
(*
- ?agent per socket connection? - make it the most recently used
o Tests
  - compare local and remote approvals
o Test enhancement
  - test as many of the interface api as possible
  - datastore api tests
o Additional enhancements
  - Logging
  - Telemetry
- Telemetry.Flush on closing solution hangs VS
- Unit tests for FileSystem
- Update FSharp.Core to 4.0
o Add other stuff in YoLo 
  - from prelude esp
  - let inline ofNull value
- Convert tests.runEngine to async {}
*)
