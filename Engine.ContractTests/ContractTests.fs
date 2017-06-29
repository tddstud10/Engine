module R4nd0mApps.TddStud10.Engine.Core.ContractTests

open ApprovalTests
open ApprovalTests.Namers
open ApprovalTests.Reporters
open ContractTestHelpers
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System
open System.IO
open Xunit

let solutions = [ 
    @"CSXUnit1xNUnit3x.NET20\CSXUnit1xNUnit3x.sln" 
    @"VBXUnit1xNUnit2x.NET40\VBXUnit1xNUnit2x.sln" 
    // @"FSXUnit2xNUnit2x.NET45\FSXUnit2xNUnit2x.sln" // NOTE: PARTHO: Code generation and therefore sequence point generation appear inconsistent, other two provide sufficient coverage
]

let variants = [ 
    "BREAK_NOTHING" 
    "BREAK_BUILD"
    "BREAK_TEST" 
]

let ``Test Data - E2E Run for Project`` : obj array seq = 
    seq { 
        for s in solutions do
            for v in variants -> s, v, (Guid.NewGuid().ToString())
    }
    |> Seq.map (fun (a, b, c) -> 
           [| box a
              box b
              box c |])

let runEngine sln props dir = 
    let ssr = sprintf @"%s\%s" binRoot dir
    let h = new TddStud10HostProxy(9999) :> ITddStud10Host
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
                  EngineConfig = { EngineConfigLoader.load (getTestProjectsRoot sln |> FilePath) with SnapShotRoot = ssr; AdditionalMSBuildProperties = props }
                  SolutionPath = getTestProjectsRoot sln |> FilePath
                  SessionStartTime = DateTime.UtcNow.AddMinutes(-1.0) }
            let! runInProgress = e.RunEngine ep
            Assert.True(runInProgress)
            do! waitWhileRunInProgress e
            do! Async.Sleep(2000)
            let! dsState = ds.GetSerializedState()
            let runOutput = sprintf "[%s\r\n,%s]" ([ eEvents; dsEvents ] |> JsonContract.serializeFormatted) dsState
            h.Dispose()
            Assert.False(h.IsRunning)
            return runOutput, ep.SolutionPath
        }
        |> Async.RunSynchronously
    finally
        h.Dispose()
        if Directory.Exists ssr then SafeExec.safeExec (fun () -> Directory.Delete(ssr, true))

[<UseReporter(typeof<DiffReporter>)>]
[<UseApprovalSubdirectory("approvals")>]
[<Theory>]
[<MemberData("Test Data - E2E Run for Project")>]
let ``E2E Run for Project`` (s : string, v : string, d : string) = 
    use __ = ApprovalResults.ForScenario(Path.GetDirectoryName(s), v)
    let output, projRoot = runEngine s [| sprintf "DefineConstants=%s" v |] d
    Approvals.Verify(output, Func<_, _>(normalizeJsonDoc binRoot (Path.GetDirectoryName(projRoot.ToString()))))

[<Fact>]
let ``DataStore API Tests``() = 
    let sln = @"CSXUnit1xNUnit3x.NET20\CSXUnit1xNUnit3x.sln"
    let props = [| "DefineConstants=BREAK_TEST" |]
    let ssr = sprintf @"%s\%O" binRoot (Guid.NewGuid())
    let h = new TddStud10HostProxy(9999) :> ITddStud10Host
    let e = h.GetEngine()
    let ds = h.GetDataStore()
    try 
        async { 
            let! _ = h.Start()
            let! _ = e.EnableEngine()
            let ep = 
                { HostVersion = HostVersion.VS2015
                  EngineConfig = { EngineConfigLoader.load (getTestProjectsRoot sln |> FilePath) with SnapShotRoot = ssr; AdditionalMSBuildProperties = props }
                  SolutionPath = getTestProjectsRoot sln |> FilePath
                  SessionStartTime = DateTime.UtcNow.AddMinutes(-1.0) }
            let! _ = e.RunEngine ep
            do! waitWhileRunInProgress e
            let srcFile = Path.Combine(Path.GetDirectoryName(getTestProjectsRoot sln), "Class1.cs") |> FilePath
            // API: GetRunStartParams
            let! rsp = ds.GetRunStartParams()
            Assert.Equal((rsp.Value.Solution.Path, rsp.Value.StartTime), (ep.SolutionPath, ep.SessionStartTime))
            // API: GetTestsInFile
            let! dltc = ds.GetTestsInFile(srcFile)
            let tcs = 
                dltc.Values
                |> Seq.collect id
                |> Seq.map (fun tc -> tc.FullyQualifiedName)
                |> Seq.sort
            Assert.Equal<string []>
                ([| "CSXUnit1xNUnit3x.StringTests3.IndexOf"
                    "CSXUnit1xNUnit3x.StringTests3.SquareRootDefinition(-1.0d)"
                    "CSXUnit1xNUnit3x.StringTests3.SquareRootDefinition(0.0d)" 
                    "CSXUnit1xNUnit3x.StringTests3.SquareRootDefinition(1.0d)" |], tcs |> Seq.toArray)
            // API: GetSequencePointsForFile
            let! sps = ds.GetSequencePointsForFile(srcFile)
            Assert.Equal(23, sps |> Seq.length)
            let! dltfi = ds.GetTestFailureInfosInFile(srcFile)
            let msgs = 
                dltfi.Values
                |> Seq.collect id
                |> Seq.map (fun tfi -> tfi.message)
                |> Seq.sort
            Assert.Equal<string []>
                ([| "Assert.Equal() Failure\r\nExpected: 7\r\nActual:   6" |], msgs |> Seq.toArray)
            // API: GetTestResultsForSequencepointsIds
            let! spidtr = ds.GetTestResultsForSequencepointsIds(sps |> Seq.map (fun sp -> sp.id))
            let trgs = 
                spidtr.Values
                |> Seq.collect id
                |> Seq.groupBy (fun tr -> tr.Outcome)
                |> dict
            Assert.Equal(3, trgs.Keys.Count)
            Assert.Equal(9, trgs.[TONone] |> Seq.length)
            Assert.Equal(10, trgs.[TOFailed] |> Seq.length)
            Assert.Equal(28, trgs.[TOPassed] |> Seq.length)
            h.Dispose()
        }
        |> Async.RunSynchronously
    finally
        h.Dispose()
        if Directory.Exists ssr then SafeExec.safeExec (fun () -> Directory.Delete(ssr, true))

let quickRun ssr (h : ITddStud10Host) = 
    let concat = Seq.fold (fun acc e -> acc + ";" + e.ToString()) ""
    async { 
        let eEvents = collectEngineEventsSummary <| h.GetEngineEvents()
        let dsEvents = collectDataStoreEvents <| h.GetDataStoreEvents()
        let! sVer = h.Start()
        let e = h.GetEngine()
        let ds = h.GetDataStore()
        let! _ = e.DisableEngine()
        let! _ = e.EnableEngine()
        let ep = 
            { HostVersion = HostVersion.VS2015
              EngineConfig = { EngineConfigLoader.defaultValue with SnapShotRoot = ssr; AdditionalMSBuildProperties = [||] }
              SolutionPath = "c:\\nonexistant.sln.xxx" |> FilePath
              SessionStartTime = DateTime.UtcNow.AddMinutes(-1.0) }
        let! _ = e.RunEngine ep
        do! waitWhileRunInProgress e
        do! Async.Sleep(2000)
        let! rsp = ds.GetRunStartParams()
        h.Dispose()
        return sVer, eEvents |> concat, dsEvents |> concat, rsp.Value.StartTime.Equals(ep.SessionStartTime)
    }

[<Fact>]
let ``Host can function after being disposed and recreated``() = 
    let ssr = sprintf @"%s\%O" binRoot (Guid.NewGuid())
    let h = new TddStud10HostProxy(9999) :> ITddStud10Host
    let ees = ";RunStateChanged;RunStarting;RunStateChanged;RunStepStarting;RunStateChanged;RunStepError;RunStateChanged;RunStepEnded;RunStateChanged;RunError;RunEnded"
    let dses = ";SequencePointsUpdated;TestCasesUpdated;TestResultsUpdated;TestFailureInfoUpdated;CoverageInfoUpdated" 

    try 
        let res = 
            h
            |> quickRun ssr
            |> Async.RunSynchronously
        Assert.Equal(res, (App.getVersion(), ees, dses, true))
        h.Dispose()
        let h = new TddStud10HostProxy(9999) :> ITddStud10Host
        
        let res = 
            h
            |> quickRun ssr
            |> Async.RunSynchronously
        Assert.Equal(res, (App.getVersion(), ees, dses, true))
    finally
        h.Dispose()
        if Directory.Exists ssr then SafeExec.safeExec (fun () -> Directory.Delete(ssr, true))
