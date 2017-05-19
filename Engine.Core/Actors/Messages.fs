namespace R4nd0mApps.TddStud10.Engine.Actors

open TestData
open R4nd0mApps.TddStud10.Engine.Core

module ActorMessages = 

    type DataStoreMessage =
        | DsInitialize

    type IdeEventsMessage =
        | EvResyncStarting of ResyncParams
        | EvRunStarting of ResyncParams
        | EvProjectsDiscovered of ResyncParams * Project[]
        | EvProjectSnapshotStarting of ResyncParams * Project
        | EvProjectSnapshotSucceeded of ResyncParams * Project
        | EvProjectSnapshotFailed of ResyncParams * Project * FailureInfo
        | EvProjectFileFixStarting of ResyncParams * Project
        | EvProjectFileFixSucceeded of ResyncParams * Project
        | EvProjectFileFixFailed of ResyncParams * Project * FailureInfo
        | EvProjectBuildStarting of ResyncParams * Project
        | EvProjectBuildSucceeded of ResyncParams * Project         
        | EvProjectBuildFailed of ResyncParams * Project * FailureInfo 
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
        | ScheduleProjectBuildSucceeded of ResyncParams * Project
        | ScheduleProjectBuildFailed of ResyncParams * Project * FailureInfo

    type ProjectBuildCoordinatorMessage = 
        | ReadyForBuildProject of ResyncParams * Project

    type ProjectSnapshotCreatorMessage = 
        | CreateProjectSnapshot of ResyncParams * Project
        | CreateProjectSnapshotSucceeded of ResyncParams * Project
        | CreateProjectSnapshotFailed of ResyncParams * Project * FailureInfo

    type ProjectFileFixerMessage = 
        | FixProjectFile of ResyncParams * Project
        | FixProjectFileSucceeded of ResyncParams * Project
        | FixProjectFileFailed of ResyncParams * Project * FailureInfo

    type ProjectBuilderMessage = 
        | BuildProject of ResyncParams * Project
        | BuildProjectSucceeded of ResyncParams * Project
        | BuildProjectFailed of ResyncParams * Project * FailureInfo

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

