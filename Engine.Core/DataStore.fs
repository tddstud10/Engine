namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine
open R4nd0mApps.TddStud10.Engine.Server
open System
open System.Collections.Generic

type IDataStore = 
    abstract GetStartParams : unit -> RunStartParams option
    abstract SetRunStartParams : RunStartParams -> unit
    abstract TestCasesUpdated : IEvent<PerDocumentLocationDTestCases>
    abstract SequencePointsUpdated : IEvent<PerDocumentSequencePoints>
    abstract TestResultsUpdated : IEvent<PerTestIdDResults>
    abstract TestFailureInfoUpdated : IEvent<PerDocumentLocationTestFailureInfo>
    abstract CoverageInfoUpdated : IEvent<PerSequencePointIdTestRunId>
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
    let tcu = Event<_>()
    let spu = Event<_>()
    let tru = Event<_>()
    let tfiu = Event<_>()
    let ciu = Event<_>()
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
            Exec.safeExec (fun () -> tcu.Trigger(x.TestCases))
        | SequencePoints(sp) -> 
            x.SequencePoints <- sp
            Exec.safeExec (fun () -> spu.Trigger(x.SequencePoints))
        | TestRunOutput(tr, tfi, ci) -> 
            x.TestResults <- tr
            Exec.safeExec (fun () -> tru.Trigger(x.TestResults))
            x.TestFailureInfo <- tfi
            Exec.safeExec (fun () -> tfiu.Trigger(x.TestFailureInfo))
            x.CoverageInfo <- ci
            Exec.safeExec (fun () -> ciu.Trigger(x.CoverageInfo))
    
    interface IDataStore with
        member x.GetStartParams() = x.RunStartParams
        member x.SetRunStartParams(rsp) : unit = x.RunStartParams <- rsp |> Some
        member __.TestCasesUpdated : IEvent<_> = tcu.Publish
        member __.SequencePointsUpdated : IEvent<_> = spu.Publish
        member __.TestResultsUpdated : IEvent<_> = tru.Publish
        member __.TestFailureInfoUpdated : IEvent<_> = tfiu.Publish
        member __.CoverageInfoUpdated : IEvent<_> = ciu.Publish
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
                |> JsonContract.serializeFormatted
            state

type IXDataStoreEvents = 
    inherit IDisposable
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
    let tcu = new Event<_>()
    let spu = new Event<_>()
    let tru = new Event<_>()
    let tfiu = new Event<_>()
    let ciu = new Event<_>()
    
    interface IXDataStoreEvents with
        member __.TestCasesUpdated = tcu.Publish
        member __.SequencePointsUpdated = spu.Publish
        member __.TestResultsUpdated = tru.Publish
        member __.TestFailureInfoUpdated = tfiu.Publish
        member __.CoverageInfoUpdated = ciu.Publish
    
    interface IXDataStoreCallback with
        member __.OnCoverageInfoUpdated() = ciu.Trigger()
        member __.OnSequencePointsUpdated() = spu.Trigger()
        member __.OnTestCasesUpdated() = tcu.Trigger()
        member __.OnTestFailureInfoUpdated() = tfiu.Trigger()
        member __.OnTestResultsUpdated() = tru.Trigger()

    interface IDisposable with
        member x.Dispose() = ()

type XDataStoreEventsSource(notify) = 
    let disposed : bool ref = ref false
    let tcu = Event<_>()
    let tcuSub = tcu.Publish |> Observable.subscribe (TestCasesUpdated >> notify)
    let spu = Event<_>()
    let spuSub = spu.Publish |> Observable.subscribe (SequencePointsUpdated >> notify)
    let tru = Event<_>()
    let truSub = tru.Publish |> Observable.subscribe (TestResultsUpdated >> notify)
    let tfiu = Event<_>()
    let tfiuSub = tfiu.Publish |> Observable.subscribe (TestFailureInfoUpdated >> notify)
    let ciu = Event<_>()
    let ciuSub = ciu.Publish |> Observable.subscribe (CoverageInfoUpdated >> notify)
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                tcuSub.Dispose()
                spuSub.Dispose()
                truSub.Dispose()
                tfiuSub.Dispose()
                ciuSub.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IXDataStoreCallback with
        member __.OnCoverageInfoUpdated() = ciu.Trigger()
        member __.OnSequencePointsUpdated() = spu.Trigger()
        member __.OnTestCasesUpdated() = tcu.Trigger()
        member __.OnTestFailureInfoUpdated() = tfiu.Trigger()
        member __.OnTestResultsUpdated() = tru.Trigger()

type XDataStoreEventsSink(o) = 
    let disposed : bool ref = ref false
    
    let tcu, tcuSub = 
        Server.createEventOfNotification<_> (function 
            | TestCasesUpdated x -> Some x
            | _ -> None) o
    
    let spu, spuSub = 
        Server.createEventOfNotification<_> (function 
            | SequencePointsUpdated x -> Some x
            | _ -> None) o
    
    let tru, truSub = 
        Server.createEventOfNotification<_> (function 
            | TestResultsUpdated x -> Some x
            | _ -> None) o
    
    let tfiu, tfiuSub = 
        Server.createEventOfNotification<_> (function 
            | TestFailureInfoUpdated x -> Some x
            | _ -> None) o
    
    let ciu, ciuSub = 
        Server.createEventOfNotification<_> (function 
            | CoverageInfoUpdated x -> Some x
            | _ -> None) o
    
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                tcuSub.Dispose()
                spuSub.Dispose()
                truSub.Dispose()
                tfiuSub.Dispose()
                ciuSub.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IXDataStoreEvents with
        member __.TestCasesUpdated = tcu.Publish
        member __.SequencePointsUpdated = spu.Publish
        member __.TestResultsUpdated = tru.Publish
        member __.TestFailureInfoUpdated = tfiu.Publish
        member __.CoverageInfoUpdated = ciu.Publish

type IXDataStore = 
    abstract GetRunStartParams : unit -> Async<RunStartParams option>
    abstract SetRunStartParams : RunStartParams -> Async<unit>
    abstract UpdateData : RunData -> Async<unit>
    abstract GetTestsInFile : fp:FilePath -> Async<IDictionary<DocumentLocation, DTestCase []>>
    abstract GetSequencePointsForFile : fp:FilePath -> Async<seq<SequencePoint>>
    abstract GetTestFailureInfosInFile : fp:FilePath -> Async<IDictionary<DocumentLocation, TestFailureInfo []>>
    abstract GetTestResultsForSequencepointsIds : spids:seq<SequencePointId>
     -> Async<IDictionary<SequencePointId, DTestResult []>>
    abstract GetSerializedState : unit -> Async<string>

type XDataStore(ds : IDataStore, cb : IXDataStoreCallback option) = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    
    let logFn n f = 
        logger.logInfof "|DATASTORE ACCESS| =====> %s" n
        f() |> Async.result
    
    let cbs : IXDataStoreCallback list ref = ref (cb |> Option.fold (fun _ e -> [ e ]) [])
    let invokeCbs f _ = !cbs |> List.iter (fun cb -> Exec.safeExec (fun () -> f cb))
    do ds.TestCasesUpdated.Add(invokeCbs (fun cb -> cb.OnTestCasesUpdated()))
    do ds.SequencePointsUpdated.Add(invokeCbs (fun cb -> cb.OnSequencePointsUpdated()))
    do ds.TestResultsUpdated.Add(invokeCbs (fun cb -> cb.OnTestResultsUpdated()))
    do ds.TestFailureInfoUpdated.Add(invokeCbs (fun cb -> cb.OnTestFailureInfoUpdated()))
    do ds.CoverageInfoUpdated.Add(invokeCbs (fun cb -> cb.OnCoverageInfoUpdated()))
    interface IXDataStore with
        member __.GetRunStartParams() = logFn "GetRunStartParams" (fun () -> ds.GetStartParams()) 
        member __.SetRunStartParams rsp = logFn "SetRunStartParams" (fun () -> ds.SetRunStartParams rsp)
        member __.UpdateData rd = logFn "SetRunStartParams" (fun () -> ds.UpdateData rd)
        member __.GetTestsInFile(fp) = logFn "FindTestsInFile" (fun () -> ds.FindTestsInFile fp)
        member __.GetTestFailureInfosInFile(fp) = 
            logFn "FindTestFailureInfosInFile" (fun () -> ds.FindTestFailureInfosInFile fp)
        member __.GetSequencePointsForFile(path : FilePath) = 
            logFn "GetSequencePointsForFile" (fun () -> ds.GetSequencePointsForFile path)
        member __.GetTestResultsForSequencepointsIds(spids) = 
            logFn "GetTestResultsForSequencepointsIds" (fun () -> ds.GetTestResultsForSequencepointsIds spids)
        member __.GetSerializedState() = logFn "GetSerializedState" (fun () -> ds.GetSerializedState())

type XDataStoreProxy(baseUrl) = 
    interface IXDataStore with
        member __.GetRunStartParams() =
            Server.getFromServer<_> baseUrl Server.UrlSubPaths.DataStoreRunStartParams
        member __.SetRunStartParams _ = failwith "Not implemented"
        member __.UpdateData _ = failwith "Not implemented"
        member __.GetTestFailureInfosInFile(fp) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreFailureInfo fp
        member __.GetTestsInFile(fp) = Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreTests fp
        member __.GetSequencePointsForFile(fp) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreSequencePoints fp
        member __.GetTestResultsForSequencepointsIds(spids) = 
            Server.postToServer<_> baseUrl Server.UrlSubPaths.DataStoreTestResultsForSequencePointIds spids
        member __.GetSerializedState() = Server.getFromServer<_> baseUrl Server.UrlSubPaths.DataStoreSerializedState
