namespace R4nd0mApps.TddStud10.Engine.Core

open Microsoft.FSharp.Reflection
open R4nd0mApps.TddStud10.Common.Domain
open System
open System.Reflection
open System.Runtime.Serialization

// =================================================
// NOTE: Adding any new cases will break RunStateTracker.
// When we get rid of the B/T style notification icon, get rid of this.
[<KnownType("KnownTypes")>]
type RunStepKind = 
    | Build
    | Test
    override t.ToString() = 
        match t with
        | Build -> "Build"
        | Test -> "Test"
    static member KnownTypes() = 
        typeof<RunStepKind>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunStepSubKind = 
    | CreateSnapshot
    | DeleteBuildOutput
    | BuildSnapshot
    | RefreshTestRuntime
    | DiscoverSequencePoints
    | DiscoverTests
    | InstrumentBinaries
    | RunTests
    static member KnownTypes() = 
        typeof<RunStepSubKind>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunStepName = 
    | RunStepName of string
    override t.ToString() = 
        match t with
        | RunStepName s -> s
    static member KnownTypes() = 
        typeof<RunStepName>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunStepStatus = 
    | Aborted
    | Succeeded
    | Failed
    override t.ToString() = 
        match t with
        | Aborted -> "Aborted"
        | Succeeded -> "Succeeded"
        | Failed -> "Failed"
    static member KnownTypes() = 
        typeof<RunStepStatus>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunStepStatusAddendum = 
    | FreeFormatData of string
    | ExceptionData of Exception
    override t.ToString() = 
        match t with
        | FreeFormatData s -> s
        | ExceptionData e -> e.ToString()
    static member KnownTypes() = 
        typeof<RunStepStatusAddendum>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunState = 
    | Initial
    | EngineErrorDetected
    | EngineError
    | FirstBuildRunning
    | BuildFailureDetected
    | BuildFailed
    | TestFailureDetected
    | TestFailed
    | BuildRunning
    | BuildPassed
    | TestRunning
    | TestPassed
    static member KnownTypes() = 
        typeof<RunState>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type RunEvent = 
    | RunStarting
    | RunStepStarting of RunStepKind
    | RunStepError of RunStepKind * RunStepStatus
    | RunStepEnded of RunStepKind * RunStepStatus
    | RunError of Exception
    static member KnownTypes() = 
        typeof<RunEvent>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<KnownType("KnownTypes")>]
type HostVersion =
    | VS2013
    | VS2015
    override x.ToString() = 
        match x with
        | VS2013 -> "12.0"
        | VS2015 -> "14.0"
    static member KnownTypes() = 
        typeof<HostVersion>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

type public IRunExecutorHost = 
    abstract HostVersion : HostVersion
    abstract CanContinue : unit -> bool
    abstract RunStateChanged : RunState -> unit

[<KnownType("KnownTypes")>]
type RunData = 
    | NoData
    | TestCases of PerDocumentLocationDTestCases
    | SequencePoints of PerDocumentSequencePoints
    | TestRunOutput of PerTestIdDResults * PerDocumentLocationTestFailureInfo * PerSequencePointIdTestRunId
    static member KnownTypes() = 
        typeof<RunData>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

[<CLIMutable>]
type RunDataFiles = 
    { SequencePointStore : FilePath
      CoverageSessionStore : FilePath
      TestResultsStore : FilePath
      DiscoveredUnitTestsStore : FilePath
      TestFailureInfoStore : FilePath
      DiscoveredUnitDTestsStore : FilePath }

[<CLIMutable>]
type SolutionPaths = 
    { Path : FilePath
      SnapshotPath : FilePath
      BuildRoot : FilePath }

[<CLIMutable>]
type RunStartParams = 
    { SnapShotRoot : FilePath
      StartTime : DateTime
      TestHostPath : FilePath
      Solution : SolutionPaths
      DataFiles : RunDataFiles
      // TODO: Merge this with EngineConfig, otherwise we will keep duplicating parameters
      IgnoredTests : string
      AdditionalMSBuildProperties : string[] }

[<CLIMutable>]
type RunStepInfo = 
    { name : RunStepName
      kind : RunStepKind
      subKind : RunStepSubKind }

[<CLIMutable>]
type RunStepResult = 
    { status : RunStepStatus
      runData : RunData
      addendum : RunStepStatusAddendum }

exception RunStepFailedException of RunStepResult

[<CLIMutable>]
type RunStepStartingEventArg = 
    { sp : RunStartParams
      info : RunStepInfo }

[<CLIMutable>]
type RunStepErrorEventArg = 
    { sp : RunStartParams
      info : RunStepInfo
      rsr : RunStepResult }

[<CLIMutable>]
type RunStepEndedEventArg = 
    { sp : RunStartParams
      info : RunStepInfo
      rsr : RunStepResult }

type RunStepEvents = 
    { onStart : Event<RunStepStartingEventArg>
      onError : Event<RunStepErrorEventArg>
      onFinish : Event<RunStepEndedEventArg> }

type RunStepFunc = IRunExecutorHost -> RunStartParams -> RunStepInfo -> RunStepEvents -> RunStepResult

type RunStepFuncWrapper = RunStepFunc -> RunStepFunc

type RunStep = 
    { info : RunStepInfo
      func : RunStepFunc }

type RunSteps = RunStep array
