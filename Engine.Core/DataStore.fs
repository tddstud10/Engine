namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common.Domain
open System.Collections.Generic
open System.ServiceModel

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
    abstract FindTestsInFile : FilePath -> IDictionary<DocumentLocation, DTestCase[]>
    abstract GetSequencePointsForFile : FilePath -> seq<SequencePoint>
    abstract FindTestFailureInfo : DocumentLocation -> seq<TestFailureInfo>
    abstract FindTestFailureInfosInFile : FilePath -> IDictionary<DocumentLocation, TestFailureInfo[]>
    abstract GetRunIdsForTestsCoveringSequencePointId : SequencePointId -> seq<TestRunId>
    abstract GetResultsForTestId : TestId -> seq<DTestResult>
    abstract GetTestResultsForSequencepointsIds : seq<SequencePointId>
     -> IDictionary<SequencePointId, DTestResult[]>

type DataStore() = 
    static let instance = Lazy.Create(fun () -> DataStore())
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
    member private x.UpdateData = function
        | NoData -> ()
        | TestCases(tc) -> 
            x.TestCases <- tc
            Common.safeExec (fun () -> testCasesUpdated.Trigger(x.TestCases))
        | SequencePoints(sp) -> 
            x.SequencePoints <- sp
            Common.safeExec (fun () -> sequencePointsUpdated.Trigger(x.SequencePoints))
        | TestRunOutput(tr, tfi, ci) -> 
            x.TestResults <- tr
            Common.safeExec (fun () -> testResultsUpdated.Trigger(x.TestResults))
            x.TestFailureInfo <- tfi
            Common.safeExec (fun () -> testFailureInfoUpdated.Trigger(x.TestFailureInfo))
            x.CoverageInfo <- ci
            Common.safeExec (fun () -> coverageInfoUpdated.Trigger(x.CoverageInfo))
        
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
            PerDocumentSequencePoints() |> SequencePoints |> x.UpdateData
            PerDocumentLocationDTestCases() |> TestCases |> x.UpdateData
            (PerTestIdDResults(), PerDocumentLocationTestFailureInfo(), PerSequencePointIdTestRunId()) |> TestRunOutput |> x.UpdateData
        
        member x.FindTest dl : DTestCase seq = (dl, x.TestCases) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        member x.FindTestsInFile file = 
            x.TestCases.Keys
            |> Seq.filter (fun dl -> dl.document = file)
            |> Seq.map (fun dl -> dl, dl |> (x :> IDataStore).FindTest |> Seq.toArray)
            |> dict
        member x.GetSequencePointsForFile p : SequencePoint seq = 
            (p, x.SequencePoints) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        member x.FindTestFailureInfo dl : TestFailureInfo seq = 
            (dl, x.TestFailureInfo) ||> Dict.tryGetValue Seq.empty (fun v -> v :> seq<_>)
        member x.FindTestFailureInfosInFile file = 
            x.TestFailureInfo.Keys
            |> Seq.filter (fun dl -> dl.document = file)
            |> Seq.map (fun dl -> dl, dl |> (x :> IDataStore).FindTestFailureInfo |> Seq.toArray)
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

    static member Instance 
        with public get () = instance.Value :> IDataStore

type IXDataStoreCallback = 
    
    [<OperationContract(IsOneWay = true)>]
    abstract OnTestCasesUpdated : unit -> unit
    
    [<OperationContract(IsOneWay = true)>]
    abstract OnSequencePointsUpdated : unit -> unit
    
    [<OperationContract(IsOneWay = true)>]
    abstract OnTestResultsUpdated : unit -> unit
    
    [<OperationContract(IsOneWay = true)>]
    abstract OnTestFailureInfoUpdated : unit -> unit
    
    [<OperationContract(IsOneWay = true)>]
    abstract OnCoverageInfoUpdated : unit -> unit

[<CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)>]
type XDataStoreEvents() = 
    let testCasesUpdated = new Event<_>()
    let sequencePointsUpdated = new Event<_>()
    let testResultsUpdated = new Event<_>()
    let testFailureInfoUpdated = new Event<_>()
    let coverageInfoUpdated = new Event<_>()
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

[<ServiceContract(CallbackContract = typeof<IXDataStoreCallback>)>]
type IXDataStore = 
    
    [<OperationContract>]
    abstract Connect : unit -> unit
    
    [<OperationContract>]
    abstract Disconnect : unit -> unit
    
    [<OperationContract>]
    abstract UpdateRunStartParams : rsp:RunStartParams -> unit
    
    [<OperationContract>]
    abstract UpdateData : rd:RunData -> unit
    
    [<OperationContract>]
    abstract ResetData : unit -> unit
    
    [<OperationContract>]
    abstract FindTest : dl:DocumentLocation -> seq<DTestCase>

    [<OperationContract>]
    abstract FindTestsInFile : fp:FilePath -> IDictionary<DocumentLocation, DTestCase[]>
    
    [<OperationContract>]
    abstract GetSequencePointsForFile : fp:FilePath -> seq<SequencePoint>
    
    [<OperationContract>]
    abstract FindTestFailureInfo : dl:DocumentLocation -> seq<TestFailureInfo>
    
    [<OperationContract>]
    abstract FindTestFailureInfosInFile : fp:FilePath -> IDictionary<DocumentLocation, TestFailureInfo[]>
    
    [<OperationContract>]
    abstract GetRunIdsForTestsCoveringSequencePointId : spid:SequencePointId -> seq<TestRunId>
    
    [<OperationContract>]
    abstract GetResultsForTestId : tid:TestId -> seq<DTestResult>

    [<OperationContract>]
    abstract GetTestResultsForSequencepointsIds : spids: seq<SequencePointId> -> IDictionary<SequencePointId, DTestResult[]>

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)>]
type XDataStore(dataStore : IDataStore, cb : IXDataStoreCallback option) = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    let logFn n f =
        logger.logInfof "|DATASTORE ACCESS| =====> %s" n
        f()

    let cbs : IXDataStoreCallback list ref = ref (cb |> Option.fold (fun _ e -> [ e ]) [])
    let invokeCbs f _ = !cbs |> List.iter (fun cb -> Common.safeExec (fun () -> f cb))
    do dataStore.TestCasesUpdated.Add(invokeCbs (fun cb -> cb.OnTestCasesUpdated()))
    do dataStore.SequencePointsUpdated.Add(invokeCbs (fun cb -> cb.OnSequencePointsUpdated()))
    do dataStore.TestResultsUpdated.Add(invokeCbs (fun cb -> cb.OnTestResultsUpdated()))
    do dataStore.TestFailureInfoUpdated.Add(invokeCbs (fun cb -> cb.OnTestFailureInfoUpdated()))
    do dataStore.CoverageInfoUpdated.Add(invokeCbs (fun cb -> cb.OnCoverageInfoUpdated()))
    new() = new XDataStore(DataStore.Instance, None)
    interface IXDataStore with

        member __.Connect() : unit = 
            let cb = OperationContext.Current.GetCallbackChannel<IXDataStoreCallback>()
            if (!cbs
                |> List.exists ((=) cb)
                |> not)
            then cbs := OperationContext.Current.GetCallbackChannel<IXDataStoreCallback>() :: !cbs
        
        member __.Disconnect() : unit = 
            let cb = OperationContext.Current.GetCallbackChannel<IXDataStoreCallback>()
            cbs := !cbs |> List.filter ((<>) cb)
        
        member __.FindTest(dl : DocumentLocation) : seq<DTestCase> = logFn "FindTest" (fun () -> dataStore.FindTest dl)
        member __.FindTestsInFile(fp) = logFn "FindTestsInFile" (fun () -> dataStore.FindTestsInFile fp)
        member __.FindTestFailureInfo(dl : DocumentLocation) : seq<TestFailureInfo> = logFn "FindTestFailureInfo" (fun () -> dataStore.FindTestFailureInfo dl)
        member __.FindTestFailureInfosInFile(fp) = logFn "FindTestFailureInfosInFile" (fun () -> dataStore.FindTestFailureInfosInFile fp)
        member __.GetResultsForTestId(tid : TestId) : seq<DTestResult> = logFn "GetResultsForTestId" (fun () -> dataStore.GetResultsForTestId tid)
        member __.GetRunIdsForTestsCoveringSequencePointId(spid : SequencePointId) : seq<TestRunId> = 
            logFn "GetRunIdsForTestsCoveringSequencePointId" (fun () -> dataStore.GetRunIdsForTestsCoveringSequencePointId spid)
        member __.GetSequencePointsForFile(path : FilePath) : seq<SequencePoint> = 
            logFn "GetSequencePointsForFile" (fun () -> dataStore.GetSequencePointsForFile path)
        member __.ResetData() : unit = logFn "ResetData" (fun () -> dataStore.ResetData())
        member __.UpdateData(rd : RunData) : unit = logFn "UpdateData" (fun () -> dataStore.UpdateData rd)
        member __.UpdateRunStartParams(rsp : RunStartParams) : unit = logFn "UpdateRunStartParams" (fun () -> dataStore.UpdateRunStartParams rsp)
        member __.GetTestResultsForSequencepointsIds(spids) = logFn "GetTestResultsForSequencepointsIds" (fun () -> dataStore.GetTestResultsForSequencepointsIds spids)
