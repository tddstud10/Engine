namespace R4nd0mApps.TddStud10.Engine.Actors

open TestData
open R4nd0mApps.TddStud10.Engine.Core

module ActorMessages = 

    type DataStoreMessage =
        | DsInitialize

    type IdeEventsMessage =
        | EvResyncStarting of ResyncParams
        | EvRunStarting of ResyncParams
        | EvProjectsDiscovered of ResyncParams * ProjectId[]
        | EvProjectSnapshotStarting of ResyncParams * ProjectId
        | EvProjectSnapshotSucceeded of ResyncParams * ProjectId
        | EvProjectSnapshotFailed of ResyncParams * ProjectId * FailureInfo
        | EvProjectFileFixStarting of ResyncParams * ProjectId
        | EvProjectFileFixSucceeded of ResyncParams * ProjectId
        | EvProjectFileFixFailed of ResyncParams * ProjectId * FailureInfo
        | EvProjectBuildStarting of ResyncParams * ProjectId
        | EvProjectBuildSucceeded of ResyncParams * ProjectId         
        | EvProjectBuildFailed of ResyncParams * ProjectId * FailureInfo 
        | EvAssemblyInstrumentationStarting of ResyncParams * AssemblyId
        | EvAssemblyInstrumentationSucceeded of ResyncParams * AssemblyId
        | EvAssemblyInstrumentationFailed of ResyncParams * AssemblyId * FailureInfo
        | EvAssemblyTestDiscoveryStarting of ResyncParams * AssemblyId
        | EvTestDiscovered of ResyncParams * TestId
        | EvAssemblyTestDiscoverySucceeded of ResyncParams * AssemblyId
        | EvAssemblyTestDiscoveryFailed of ResyncParams * AssemblyId * FailureInfo
        | EvAssemblySequencePointsDiscoveryStarting of ResyncParams * AssemblyId
        | EvSequencePointsDiscovered of ResyncParams * TestId
        | EvAssemblySequencePointsDiscoverySucceeded of ResyncParams * AssemblyId
        | EvAssemblySequencePointsDiscoveryFailed of ResyncParams * AssemblyId * FailureInfo
        | EvTestRunStarting of ResyncParams * TestId
        | EvTestRunSucceeded of ResyncParams * TestId
        | EvTestRunFailed of ResyncParams * TestId * FailureInfo

    type RunnerMessage = 
        | Resync of ResyncParams
        | Run of ResyncParams
        | CancelRun

    type RunSchedulerMessage = 
        | ScheduleProjectBuild of ResyncParams * Solution
        | ScheduleProjectBuildSucceeded of ResyncParams * ProjectId
        | ScheduleProjectBuildFailed of ResyncParams * ProjectId * FailureInfo

    type ProjectBuildCoordinatorMessage = 
        | ReadyForBuildProject of ResyncParams * ProjectId

    type ProjectSnapshotCreatorMessage = 
        | CreateProjectSnapshot of ResyncParams * ProjectId
        | CreateProjectSnapshotSucceeded of ResyncParams * ProjectId
        | CreateProjectSnapshotFailed of ResyncParams * ProjectId * FailureInfo

    type ProjectFileFixerMessage = 
        | FixProjectFile of ResyncParams * ProjectId
        | FixProjectFileSucceeded of ResyncParams * ProjectId
        | FixProjectFileFailed of ResyncParams * ProjectId * FailureInfo

    type ProjectBuilderMessage = 
        | BuildProject of ResyncParams * ProjectId
        | BuildProjectSucceeded of ResyncParams * ProjectId
        | BuildProjectFailed of ResyncParams * ProjectId * FailureInfo

    type AssemblyInstrumenterMessage = 
        | InstrumentAssembly of ResyncParams * AssemblyId
        | InstrumentAssemblySucceeded of ResyncParams * AssemblyId
        | InstrumentAssemblyFailed of ResyncParams * AssemblyId * FailureInfo

    type AssemblyTestsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblyTests of ResyncParams * AssemblyId

    type AssemblyTestsDiscovererMessage = 
        | DiscoverAssemblyTests of ResyncParams * AssemblyId
        | DiscoverAssemblyTestsSucceeded of ResyncParams * AssemblyId
        | DiscoverAssemblyTestsFailed of ResyncParams * AssemblyId * FailureInfo

    type AssemblySequencePointsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblySequencePoints of ResyncParams * AssemblyId

    type AssemblySequencePointsDiscovererMessage = 
        | DiscoverAssemblySequencePoints of ResyncParams * AssemblyId
        | DiscoverAssemblySequencePointsSucceeded of ResyncParams * AssemblyId
        | DiscoverAssemblySequencePointsFailed of ResyncParams * AssemblyId * FailureInfo

    type TestRunnerCoordinatorMessage =
        | ReadyForRunTest of ResyncParams * TestId

    type TestRunnerMessage =
        | RunTest of ResyncParams * TestId
        | RunTestSucceeded of ResyncParams * TestId
        | RunTestFailed of ResyncParams * TestId * FailureInfo

