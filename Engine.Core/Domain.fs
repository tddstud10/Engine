namespace R4nd0mApps.TddStud10.Engine.Core

open Microsoft.FSharp.Reflection
open R4nd0mApps.TddStud10.Common.Domain
open System
open System.ComponentModel
open System.Reflection
open System.Runtime.Serialization
open System.Collections.Generic

type EngineConfig = 
    { [<DefaultValue(@"%temp%\_tdd")>]            
      SnapShotRoot : string
      [<DefaultValue("")>]            
      IgnoredTests : string
      [<DefaultValue(false)>]            
      IsDisabled : bool
      [<DefaultValue([|"_TDDSTUD10"|])>]            
      AdditionalMSBuildProperties : string[]
      [<DefaultValue([|"packages"; "paket-files"|])>]  
      SnapshotIncludeFolders : string[]
      [<DefaultValue([|"\\.git\\"; "\\obj\\"; "\\bin\\"|])>] 
      SnapshotExcludePatterns : string[] }

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
    { StartTime : DateTime
      TestHostPath : FilePath
      Solution : SolutionPaths
      DataFiles : RunDataFiles
      Config : EngineConfig }

type ResyncId = Guid

type ResyncParams = 
    { Id : ResyncId
      SolutionPaths : SolutionPaths
      Config : EngineConfig }

type Project = 
    { Index : int // change to ID
      Name : string
      FileName : FilePath
      DirectoryName : FilePath
      Id : Guid
      Type : Guid
      Items : FilePath[] }
    override it.ToString() = 
        sprintf "%s::%O\%O [%O]#%d: Items = %d" it.Name it.DirectoryName it.FileName it.Id it.Index (it.Items |> Seq.length)
    member it.Path =
        (it.DirectoryName.ToString(), it.FileName.ToString()) ||> Path.combine |> FilePath

type Solution = 
    { Path : FilePath
      Projects : Project[]
      DependencyMap : IReadOnlyDictionary<Project, seq<Project>> }    
    override it.ToString() = 
        sprintf "%O (Dependencies: %d)" it.Path it.DependencyMap.Count

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
type RunFailureInfo =
    { Message : string
      StackTrace : string
      Result : RunStepResult option } 
    static member FromExeption (e : Exception) =
        let r = 
            match e with
            | :? RunStepFailedException as rsfe -> Some rsfe.Data0
            | _ -> None
        { Message = e.Message
          StackTrace = e.StackTrace
          Result = r } 

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
