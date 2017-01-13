namespace R4nd0mApps.TddStud10.Engine.Core

open Newtonsoft.Json
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine
open System
open System.Collections.Generic

type IDataStore = 
    abstract RunStartParams : RunStartParams option
    abstract TestCasesUpdated : IEvent<PerDocumentLocationDTestCases>
    abstract SequencePointsUpdated : IEvent<PerDocumentSequencePoints>
    abstract TestResultsUpdated : IEvent<PerTestIdDResults>
    abstract TestFailureInfoUpdated : IEvent<PerDocumentLocationTestFailureInfo>
    abstract CoverageInfoUpdated : IEvent<PerSequencePointIdTestRunId>
    abstract UpdateRunStartParams : RunStartParams -> unit
    abstract UpdateData : RunData -> unit
    abstract ResetData : unit -> unit
    abstract FindTest : DocumentLocation -> seq<DTestCase>
    abstract FindTestsInFile : FilePath -> IDictionary<DocumentLocation, DTestCase []>
    abstract GetSequencePointsForFile : FilePath -> seq<SequencePoint>
    abstract FindTestFailureInfo : DocumentLocation -> seq<TestFailureInfo>
    abstract FindTestFailureInfosInFile : FilePath -> IDictionary<DocumentLocation, TestFailureInfo []>
    abstract GetRunIdsForTestsCoveringSequencePointId : SequencePointId -> seq<TestRunId>
    abstract GetResultsForTestId : TestId -> seq<DTestResult>
    abstract GetTestResultsForSequencepointsIds : seq<SequencePointId> -> IDictionary<SequencePointId, DTestResult []>
    abstract GetSerializedState : unit -> string

type DataStore() = 
    let testCasesUpdated = Event<_>()
    let sequencePointsUpdated = Event<_>()
    let testResultsUpdated = Event<_>()
    let testFailureInfoUpdated = Event<_>()
    let coverageInfoUpdated = Event<_>()
    member val RunStartParams = None with get, set
    member val TestCases = PerDocumentLocationDTestCases() with get, set
    member val SequencePoints = PerDocumentSequencePoints() with get, set
    member val TestResults = PerTestIdDResults() with get, set
    member val TestFailureInfo = PerDocumentLocationTestFailureInfo() with get, set
    member val CoverageInfo = PerSequencePointIdTestRunId() with get, set
    
    member private x.UpdateData = 
        function 
        | NoData -> ()
        | TestCases(tc) -> 
            x.TestCases <- tc
            Exec.safeExec (fun () -> testCasesUpdated.Trigger(x.TestCases))
        | SequencePoints(sp) -> 
            x.SequencePoints <- sp
            Exec.safeExec (fun () -> sequencePointsUpdated.Trigger(x.SequencePoints))
        | TestRunOutput(tr, tfi, ci) -> 
            x.TestResults <- tr
            Exec.safeExec (fun () -> testResultsUpdated.Trigger(x.TestResults))
            x.TestFailureInfo <- tfi
            Exec.safeExec (fun () -> testFailureInfoUpdated.Trigger(x.TestFailureInfo))
            x.CoverageInfo <- ci
            Exec.safeExec (fun () -> coverageInfoUpdated.Trigger(x.CoverageInfo))
    
    interface IDataStore with
        member x.RunStartParams : RunStartParams option = x.RunStartParams
        member __.TestCasesUpdated : IEvent<_> = testCasesUpdated.Publish
        member __.SequencePointsUpdated : IEvent<_> = sequencePointsUpdated.Publish
        member __.TestResultsUpdated : IEvent<_> = testResultsUpdated.Publish
        member __.TestFailureInfoUpdated : IEvent<_> = testFailureInfoUpdated.Publish
        member __.CoverageInfoUpdated : IEvent<_> = coverageInfoUpdated.Publish
        member x.UpdateRunStartParams(rsp : RunStartParams) : unit = x.RunStartParams <- rsp |> Some
        member x.UpdateData(rd : RunData) : unit = x.UpdateData rd
        
        member x.ResetData() = 
            PerDocumentSequencePoints()
            |> SequencePoints
            |> x.UpdateData
            PerDocumentLocationDTestCases()
            |> TestCases
            |> x.UpdateData
            (PerTestIdDResults(), PerDocumentLocationTestFailureInfo(), PerSequencePointIdTestRunId())
            |> TestRunOutput
            |> x.UpdateData
        
        member x.FindTest dl : DTestCase seq = (dl, x.TestCases) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        
        member x.FindTestsInFile file = 
            x.TestCases.Keys
            |> Seq.filter (fun dl -> dl.document = file)
            |> Seq.map (fun dl -> 
                   dl, 
                   dl
                   |> (x :> IDataStore).FindTest
                   |> Seq.toArray)
            |> dict
        
        member x.GetSequencePointsForFile p : SequencePoint seq = 
            (p, x.SequencePoints) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        member x.FindTestFailureInfo dl : TestFailureInfo seq = 
            (dl, x.TestFailureInfo) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        
        member x.FindTestFailureInfosInFile file = 
            x.TestFailureInfo.Keys
            |> Seq.filter (fun dl -> dl.document = file)
            |> Seq.map (fun dl -> 
                   dl, 
                   dl
                   |> (x :> IDataStore).FindTestFailureInfo
                   |> Seq.toArray)
            |> dict
        
        member x.GetRunIdsForTestsCoveringSequencePointId spid = 
            (spid, x.CoverageInfo) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        member x.GetResultsForTestId tid = (tid, x.TestResults) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        
        member x.GetTestResultsForSequencepointsIds spids = 
            spids
            |> Seq.map (fun spid -> 
                   spid, 
                   spid
                   |> (x :> IDataStore).GetRunIdsForTestsCoveringSequencePointId
                   |> Seq.map (fun rid -> rid.testId)
                   |> Seq.distinct
                   |> Seq.map (x :> IDataStore).GetResultsForTestId
                   |> Seq.collect id
                   |> Seq.toArray)
            |> dict
        
        member i.GetSerializedState() = 
            let cfg = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
            let toJson o = JsonConvert.SerializeObject(o, Formatting.Indented, cfg)
            
            let state = 
                [ i.RunStartParams :> obj
                  (i.TestCases.ToArray() |> Array.sortBy (fun it -> it.Key.ToString())) :> obj
                  (i.SequencePoints.ToArray() |> Array.sortBy (fun it -> it.Key.ToString())) :> obj
                  (i.TestResults.ToArray() |> Array.sortBy (fun it -> it.Key.ToString())) :> obj
                  (i.TestFailureInfo.ToArray() |> Array.sortBy (fun it -> it.Key.ToString())) :> obj
                  (i.CoverageInfo.ToArray()
                   |> Array.collect 
                          (fun kv -> 
                          kv.Value.ToArray() |> Array.map (fun v -> (kv.Key.methodId.mdTokenRid, kv.Key.uid), v.testId))
                   |> Array.sortBy (fun (um, tid : TestId) -> sprintf "%O.%O" um tid)) :> obj ]
                |> toJson
            state

type IXDataStoreEvents = 
    abstract TestCasesUpdated : IEvent<unit>
    abstract SequencePointsUpdated : IEvent<unit>
    abstract TestResultsUpdated : IEvent<unit>
    abstract TestFailureInfoUpdated : IEvent<unit>
    abstract CoverageInfoUpdated : IEvent<unit>

type IXDataStoreCallback = 
    abstract OnTestCasesUpdated : unit -> unit
    abstract OnSequencePointsUpdated : unit -> unit
    abstract OnTestResultsUpdated : unit -> unit
    abstract OnTestFailureInfoUpdated : unit -> unit
    abstract OnCoverageInfoUpdated : unit -> unit

type XDataStoreEventsLocal() = 
    let testCasesUpdated = new Event<_>()
    let sequencePointsUpdated = new Event<_>()
    let testResultsUpdated = new Event<_>()
    let testFailureInfoUpdated = new Event<_>()
    let coverageInfoUpdated = new Event<_>()
    interface IXDataStoreEvents with
        member __.TestCasesUpdated = testCasesUpdated.Publish
        member __.SequencePointsUpdated = sequencePointsUpdated.Publish
        member __.TestResultsUpdated = testResultsUpdated.Publish
        member __.TestFailureInfoUpdated = testFailureInfoUpdated.Publish
        member __.CoverageInfoUpdated = coverageInfoUpdated.Publish
    interface IXDataStoreCallback with
        member __.OnCoverageInfoUpdated() = coverageInfoUpdated.Trigger()
        member __.OnSequencePointsUpdated() = sequencePointsUpdated.Trigger()
        member __.OnTestCasesUpdated() = testCasesUpdated.Trigger()
        member __.OnTestFailureInfoUpdated() = testFailureInfoUpdated.Trigger()
        member __.OnTestResultsUpdated() = testResultsUpdated.Trigger()

(*
type XDataStoreEventsSource(ns) = 
    let disposed : bool ref = ref false
    let testCasesUpdated, testCasesUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Source ns "TestCasesUpdated"
    let sequencePointsUpdated, sequencePointsUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Source ns "SequencePointsUpdated"
    let testResultsUpdated, testResultsUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Source ns "TestResultsUpdated"
    let testFailureInfoUpdated, testFailureInfoUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Source ns "TestFailureInfoUpdated"
    let coverageInfoUpdated, coverageInfoUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Source ns "CoverageInfoUpdated"
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                testCasesUpdatedDisp.Dispose()
                sequencePointsUpdatedDisp.Dispose()
                testResultsUpdatedDisp.Dispose()
                testFailureInfoUpdatedDisp.Dispose()
                coverageInfoUpdatedDisp.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IXDataStoreEvents with
        member __.TestCasesUpdated = testCasesUpdated.Publish
        member __.SequencePointsUpdated = sequencePointsUpdated.Publish
        member __.TestResultsUpdated = testResultsUpdated.Publish
        member __.TestFailureInfoUpdated = testFailureInfoUpdated.Publish
        member __.CoverageInfoUpdated = coverageInfoUpdated.Publish
    
    interface IXDataStoreCallback with
        member __.OnCoverageInfoUpdated() = coverageInfoUpdated.Trigger()
        member __.OnSequencePointsUpdated() = sequencePointsUpdated.Trigger()
        member __.OnTestCasesUpdated() = testCasesUpdated.Trigger()
        member __.OnTestFailureInfoUpdated() = testFailureInfoUpdated.Trigger()
        member __.OnTestResultsUpdated() = testResultsUpdated.Trigger()

type XDataStoreEventsSink(ns) = 
    let disposed : bool ref = ref false
    let testCasesUpdated, testCasesUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Sink ns "TestCasesUpdated"
    let sequencePointsUpdated, sequencePointsUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Sink ns "SequencePointsUpdated"
    let testResultsUpdated, testResultsUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Sink ns "TestResultsUpdated"
    let testFailureInfoUpdated, testFailureInfoUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Sink ns "TestFailureInfoUpdated"
    let coverageInfoUpdated, coverageInfoUpdatedDisp = 
        RemoteEvents.prepareEvent<unit> RemoteEvents.Type.Sink ns "CoverageInfoUpdated"
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                testCasesUpdatedDisp.Dispose()
                sequencePointsUpdatedDisp.Dispose()
                testResultsUpdatedDisp.Dispose()
                testFailureInfoUpdatedDisp.Dispose()
                coverageInfoUpdatedDisp.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IXDataStoreEvents with
        member __.TestCasesUpdated = testCasesUpdated.Publish
        member __.SequencePointsUpdated = sequencePointsUpdated.Publish
        member __.TestResultsUpdated = testResultsUpdated.Publish
        member __.TestFailureInfoUpdated = testFailureInfoUpdated.Publish
        member __.CoverageInfoUpdated = coverageInfoUpdated.Publish
*)

type IXDataStore = 
    abstract GetTestsInFile : fp:FilePath -> Async<IDictionary<DocumentLocation, DTestCase []>>
    abstract GetSequencePointsForFile : fp:FilePath -> Async<seq<SequencePoint>>
    abstract GetTestFailureInfosInFile : fp:FilePath -> Async<IDictionary<DocumentLocation, TestFailureInfo []>>
    abstract GetTestResultsForSequencepointsIds : spids:seq<SequencePointId>
     -> Async<IDictionary<SequencePointId, DTestResult []>>
    abstract GetSerializedState : unit -> Async<string>

type XDataStore(dataStore : IDataStore, cb : IXDataStoreCallback option) = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    
    let logFn n f = 
        logger.logInfof "|DATASTORE ACCESS| =====> %s" n
        f() |> Async.result
    
    let cbs : IXDataStoreCallback list ref = ref (cb |> Option.fold (fun _ e -> [ e ]) [])
    let invokeCbs f _ = !cbs |> List.iter (fun cb -> Exec.safeExec (fun () -> f cb))
    do dataStore.TestCasesUpdated.Add(invokeCbs (fun cb -> cb.OnTestCasesUpdated()))
    do dataStore.SequencePointsUpdated.Add(invokeCbs (fun cb -> cb.OnSequencePointsUpdated()))
    do dataStore.TestResultsUpdated.Add(invokeCbs (fun cb -> cb.OnTestResultsUpdated()))
    do dataStore.TestFailureInfoUpdated.Add(invokeCbs (fun cb -> cb.OnTestFailureInfoUpdated()))
    do dataStore.CoverageInfoUpdated.Add(invokeCbs (fun cb -> cb.OnCoverageInfoUpdated()))
    interface IXDataStore with
        member __.GetTestsInFile(fp) = logFn "FindTestsInFile" (fun () -> dataStore.FindTestsInFile fp)
        member __.GetTestFailureInfosInFile(fp) = 
            logFn "FindTestFailureInfosInFile" (fun () -> dataStore.FindTestFailureInfosInFile fp)
        member __.GetSequencePointsForFile(path : FilePath) = 
            logFn "GetSequencePointsForFile" (fun () -> dataStore.GetSequencePointsForFile path)
        member __.GetTestResultsForSequencepointsIds(spids) = 
            logFn "GetTestResultsForSequencepointsIds" (fun () -> dataStore.GetTestResultsForSequencepointsIds spids)
        member __.GetSerializedState() = logFn "GetSerializedState" (fun () -> dataStore.GetSerializedState())

type XDataStoreProxy(baseUrl) = 
    interface IXDataStore with
        member __.GetTestFailureInfosInFile(fp) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreFailureInfo fp
        member __.GetTestsInFile(fp) = Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreTests fp
        member __.GetSequencePointsForFile(fp) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreSequencePoints fp
        member __.GetTestResultsForSequencepointsIds(spids) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreTestResultsForSequencePointIds spids
        member __.GetSerializedState() = Server.getFromServer<_> baseUrl Server.UrlSubPaths.DataStoreSerializedState
