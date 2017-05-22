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
        | EvProjectSnapshotSucceeded of ResyncParams * ProjectSnapshotCreatorOutput
        | EvProjectSnapshotFailed of ResyncParams * Project * FailureInfo
        | EvProjectFileFixStarting of ResyncParams * ProjectSnapshotCreatorOutput
        | EvProjectFileFixSucceeded of ResyncParams * ProjectFileFixerOutput
        | EvProjectFileFixFailed of ResyncParams * ProjectSnapshotCreatorOutput * FailureInfo
        | EvProjectBuildStarting of ResyncParams * ProjectFileFixerOutput
        | EvProjectBuildSucceeded of ResyncParams * ProjectBuilderOutput
        | EvProjectBuildFailed of ResyncParams * ProjectFileFixerOutput * FailureInfo 
        | EvAssemblyInstrumentationStarting of ResyncParams * ProjectBuilderOutput
        | EvAssemblyInstrumentationSucceeded of ResyncParams * AssemblyInstrumenterOutput
        | EvAssemblyInstrumentationFailed of ResyncParams * ProjectBuilderOutput * FailureInfo
        | EvAssemblyTestDiscoveryStarting of ResyncParams * ProjectBuilderOutput
        | EvTestDiscovered of ResyncParams * TestId
        | EvAssemblyTestDiscoverySucceeded of ResyncParams * ProjectBuilderOutput
        | EvAssemblyTestDiscoveryFailed of ResyncParams * ProjectBuilderOutput * FailureInfo
        | EvAssemblySequencePointsDiscoveryStarting of ResyncParams * ProjectBuilderOutput
        | EvSequencePointsDiscovered of ResyncParams * TestId
        | EvAssemblySequencePointsDiscoverySucceeded of ResyncParams * ProjectBuilderOutput
        | EvAssemblySequencePointsDiscoveryFailed of ResyncParams * ProjectBuilderOutput * FailureInfo
        | EvTestRunStarting of ResyncParams * TestId
        | EvTestRunSucceeded of ResyncParams * TestId
        | EvTestRunFailed of ResyncParams * TestId * FailureInfo

    type RunnerMessage = 
        | Resync of ResyncParams
        | Run of ResyncParams
        | CancelRun

    type RunSchedulerMessage = 
        | ScheduleProjectBuild of ResyncParams * Solution
        | ScheduleProjectBuildSucceeded of ResyncParams * AssemblyInstrumenterOutput
        | ScheduleProjectBuildFailed of ResyncParams * Project * FailureInfo

    type ProjectBuildCoordinatorMessage = 
        | ReadyForBuildProject of ResyncParams * Project

    type ProjectSnapshotCreatorMessage = 
        | CreateProjectSnapshot of ResyncParams * Project
        | CreateProjectSnapshotSucceeded of ResyncParams * ProjectSnapshotCreatorOutput
        | CreateProjectSnapshotFailed of ResyncParams * Project * FailureInfo

    type ProjectFileFixerMessage = 
        | FixProjectFile of ResyncParams * ProjectSnapshotCreatorOutput
        | FixProjectFileSucceeded of ResyncParams * ProjectFileFixerOutput
        | FixProjectFileFailed of ResyncParams * ProjectSnapshotCreatorOutput * FailureInfo

    type ProjectBuilderMessage = 
        | BuildProject of ResyncParams * ProjectFileFixerOutput
        | BuildProjectSucceeded of ResyncParams * ProjectBuilderOutput
        | BuildProjectFailed of ResyncParams * ProjectFileFixerOutput * FailureInfo

    type AssemblyInstrumenterMessage = 
        | InstrumentAssembly of ResyncParams * ProjectBuilderOutput
        | InstrumentAssemblySucceeded of ResyncParams * AssemblyInstrumenterOutput
        | InstrumentAssemblyFailed of ResyncParams * ProjectBuilderOutput * FailureInfo

    type AssemblyTestsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblyTests of ResyncParams * ProjectBuilderOutput

    type AssemblyTestsDiscovererMessage = 
        | DiscoverAssemblyTests of ResyncParams * ProjectBuilderOutput
        | DiscoverAssemblyTestsSucceeded of ResyncParams * ProjectBuilderOutput
        | DiscoverAssemblyTestsFailed of ResyncParams * ProjectBuilderOutput * FailureInfo

    type AssemblySequencePointsDiscovererCoordinatorMessage = 
        | ReadyForDiscoverAssemblySequencePoints of ResyncParams * ProjectBuilderOutput

    type AssemblySequencePointsDiscovererMessage = 
        | DiscoverAssemblySequencePoints of ResyncParams * ProjectBuilderOutput
        | DiscoverAssemblySequencePointsSucceeded of ResyncParams * ProjectBuilderOutput
        | DiscoverAssemblySequencePointsFailed of ResyncParams * ProjectBuilderOutput * FailureInfo

    type TestRunnerCoordinatorMessage =
        | ReadyForRunTest of ResyncParams * TestId

    type TestRunnerMessage =
        | RunTest of ResyncParams * TestId
        | RunTestSucceeded of ResyncParams * TestId
        | RunTestFailed of ResyncParams * TestId * FailureInfo

