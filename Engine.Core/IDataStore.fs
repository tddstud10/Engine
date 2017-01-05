namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common.Domain
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
    abstract FindTestsInFile : FilePath -> IDictionary<DocumentLocation, DTestCase[]>
    abstract GetSequencePointsForFile : FilePath -> seq<SequencePoint>
    abstract FindTestFailureInfo : DocumentLocation -> seq<TestFailureInfo>
    abstract FindTestFailureInfosInFile : FilePath -> IDictionary<DocumentLocation, TestFailureInfo[]>
    abstract GetRunIdsForTestsCoveringSequencePointId : SequencePointId -> seq<TestRunId>
    abstract GetResultsForTestId : TestId -> seq<DTestResult>
    abstract GetTestResultsForSequencepointsIds : seq<SequencePointId>
     -> IDictionary<SequencePointId, DTestResult[]>
