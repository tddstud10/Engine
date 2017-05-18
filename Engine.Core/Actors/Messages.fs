namespace R4nd0mApps.TddStud10.Engine.Actors

open TestData

module ActorMessages = 
    type DataStoreMessage =
        | DsInitialize

    type IdeEventsMessage =
        | EvResyncStarting of RunId
        | EvRunStarting of RunId
        | EvProjectsDiscovered of RunId * ProjectId[]
        | EvProjectSnapshotStarting of RunId * ProjectId
        | EvProjectSnapshotSucceeded of RunId * ProjectId
        | EvProjectSnapshotFailed of RunId * ProjectId * FailureInfo
        | EvProjectFileFixStarting of RunId * ProjectId
        | EvProjectFileFixSucceeded of RunId * ProjectId
        | EvProjectFileFixFailed of RunId * ProjectId * FailureInfo
        | EvProjectBuildStarting of RunId * ProjectId
        | EvProjectBuildSucceeded of RunId * ProjectId         
        | EvProjectBuildFailed of RunId * ProjectId * FailureInfo 
        | EvAssemblyInstrumentationStarting of RunId * AssemblyId
        | EvAssemblyInstrumentationSucceeded of RunId * AssemblyId
        | EvAssemblyInstrumentationFailed of RunId * AssemblyId * FailureInfo
        | EvAssemblyTestDiscoveryStarting of RunId * AssemblyId
        | EvTestDiscovered of RunId * TestId
        | EvAssemblyTestDiscoverySucceeded of RunId * AssemblyId
        | EvAssemblyTestDiscoveryFailed of RunId * AssemblyId * FailureInfo
        | EvAssemblySequencePointsDiscoveryStarting of RunId * AssemblyId
        | EvSequencePointsDiscovered of RunId * TestId
        | EvAssemblySequencePointsDiscoverySucceeded of RunId * AssemblyId
        | EvAssemblySequencePointsDiscoveryFailed of RunId * AssemblyId * FailureInfo
        | EvTestRunStarting of RunId * TestId
        | EvTestRunSucceeded of RunId * TestId
        | EvTestRunFailed of RunId * TestId * FailureInfo

    type RunnerMessage = 
        | Resync of RunId * Solution
        | Run of RunId
        | CancelRun

    type RunSchedulerMessage = 
        | ScheduleProjectBuild of RunId * Solution
        | ScheduleProjectBuildSucceeded of RunId * ProjectId
        | ScheduleProjectBuildFailed of RunId * ProjectId * FailureInfo

    type ProjectBuildCoordinatorMessage = 
        | ReadyForBuildProject of RunId * ProjectId

    type ProjectSnapshotCreatorMessage = 
        | CreateProjectSnapshot of RunId * ProjectId
        | CreateProjectSnapshotSucceeded of RunId * ProjectId
        | CreateProjectSnapshotFailed of RunId * ProjectId * FailureInfo

    type ProjectFileFixerMessage = 
        | FixProjectFile of RunId * ProjectId
        | FixProjectFileSucceeded of RunId * ProjectId
        | FixProjectFileFailed of RunId * ProjectId * FailureInfo

    type ProjectBuilderMessage = 
        | BuildProject of RunId * ProjectId
        | BuildProjectSucceeded of RunId * ProjectId
        | BuildProjectFailed of RunId * ProjectId * FailureInfo

    type AssemblyInstrumenterMessage = 
        | InstrumentAssembly of RunId * AssemblyId
        | InstrumentAssemblySucceeded of RunId * AssemblyId
        | InstrumentAssemblyFailed of RunId * AssemblyId * FailureInfo

    type AssemblyTestsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblyTests of RunId * AssemblyId

    type AssemblyTestsDiscovererMessage = 
        | DiscoverAssemblyTests of RunId * AssemblyId
        | DiscoverAssemblyTestsSucceeded of RunId * AssemblyId
        | DiscoverAssemblyTestsFailed of RunId * AssemblyId * FailureInfo

    type AssemblySequencePointsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblySequencePoints of RunId * AssemblyId

    type AssemblySequencePointsDiscovererMessage = 
        | DiscoverAssemblySequencePoints of RunId * AssemblyId
        | DiscoverAssemblySequencePointsSucceeded of RunId * AssemblyId
        | DiscoverAssemblySequencePointsFailed of RunId * AssemblyId * FailureInfo

    type TestRunnerCoordinatorMessage =
        | ReadyForRunTest of RunId * TestId

    type TestRunnerMessage =
        | RunTest of RunId * TestId
        | RunTestSucceeded of RunId * TestId
        | RunTestFailed of RunId * TestId * FailureInfo

