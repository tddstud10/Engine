namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.Engine.RunSteps

module AssemblyInstrumenter = 
    open ActorMessages

    let instrumentAssemblyErrorHandler rid a (es : EventStream) (self : IActorRef) =
        async {
            let! res = Async.Catch <| AssemblyInstrumenter.instrumentAssembly rid a
            match res with
            | Choice1Of2 r -> 
                (rid, r) |> ReadyForDiscoverAssemblyTests |> es.Publish
                (rid, r) |> ReadyForDiscoverAssemblySequencePoints |> es.Publish
                (rid, r) |> ScheduleProjectBuildSucceeded |> es.Publish
                (rid, r) |> InstrumentAssemblySucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, a.Project, fi) |> ScheduleProjectBuildFailed |> es.Publish
                (rid, a, fi) |> InstrumentAssemblyFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | InstrumentAssembly(id, a) -> 
                    (id, a) |> EvAssemblyInstrumentationStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (instrumentAssemblyErrorHandler id a m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | InstrumentAssemblySucceeded(id, a) ->
                    (id, a) |> EvAssemblyInstrumentationSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | InstrumentAssemblyFailed(id, a, e) ->
                    (id, a, e) |> EvAssemblyInstrumentationFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module ProjectBuilder = 
    open ActorMessages
    open R4nd0mApps.TddStud10.Engine.Core
    
    let buildProjectErrorHandler rid (sso : ProjectSnapshotCreatorOutput) (ai : IActorRef) (es : EventStream) (self : IActorRef) =
        async {
            let! res = Async.Catch <| ProjectBuilder.buildProject rid sso
            match res with
            | Choice1Of2 r -> 
                (rid, r) |> InstrumentAssembly |> ai.Tell
                (rid, r) |> BuildProjectSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, sso.Project, fi) |> ScheduleProjectBuildFailed |> es.Publish
                (rid, sso, fi) |> BuildProjectFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (ai: IActorRef) (m : Actor<_>) =
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | BuildProject(rid, p) -> 
                    (rid, p) |> EvProjectBuildStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (buildProjectErrorHandler rid p ai m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | BuildProjectSucceeded(rid, p) ->
                    (rid, p) |> EvProjectBuildSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | BuildProjectFailed(rid, p, e) ->
                    (rid, p, e) |> EvProjectBuildFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module ProjectFileFixer = 
    open ActorMessages

    let fixProjectFileErrorHandler rid p (pb : IActorRef) (es : EventStream) (self : IActorRef) =
        async {
            let! res = Async.Catch <| ProjectFileFixer.fixProjectFile rid p
            match res with
            | Choice1Of2 r -> 
                (rid, r) |> BuildProject |> pb.Tell
                (rid, r) |> FixProjectFileSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, p.Project, fi) |> ScheduleProjectBuildFailed |> es.Publish
                (rid, p, fi) |> FixProjectFileFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (pb: IActorRef) (m : Actor<_>) = 
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | FixProjectFile(rid, p) -> 
                    (rid, p) |> EvProjectFileFixStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (fixProjectFileErrorHandler rid p pb m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | FixProjectFileSucceeded(rid, p) ->
                    (rid, p) |> EvProjectFileFixSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | FixProjectFileFailed(rid, p, e) ->
                    (rid, p, e) |> EvProjectFileFixFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module ProjectSnapshotCreator = 
    open ActorMessages

    let createProjectSnapshotErrorHandler rsp p (pff : IActorRef) (es : EventStream) (self : IActorRef) =
        async {
            let! res = Async.Catch <| ProjectSnapshotCreator.createProjectSnapshot rsp p
            match res with
            | Choice1Of2 r -> 
                (rsp, r) |> FixProjectFile |> pff.Tell
                (rsp, r) |> CreateProjectSnapshotSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rsp, p, fi) |> ScheduleProjectBuildFailed |> es.Publish
                (rsp, p, fi) |> CreateProjectSnapshotFailed |> self.Tell
        } |> Async.RunSynchronously

    let actorLoop (pff: IActorRef) (m : Actor<_>) = 
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | CreateProjectSnapshot(rid, p) ->  
                    (rid, p) |> EvProjectSnapshotStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (createProjectSnapshotErrorHandler rid p pff m.Context.System.EventStream) m.Self
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | CreateProjectSnapshotSucceeded(rid, p) ->
                    (rid, p) |> EvProjectSnapshotSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | CreateProjectSnapshotFailed(rid, p, e) ->
                    (rid, p, e) |> EvProjectSnapshotFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module ProjectBuildStepsFactory = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) = 
        let ai = spawn m.Context ActorNames.AssemblyInstrumenter.Name AssemblyInstrumenter.actorLoop
        let pb = spawn m.Context ActorNames.ProjectBuilder.Name (ProjectBuilder.actorLoop ai)
        let pff = spawn m.Context ActorNames.ProjectFileFixer.Name (ProjectFileFixer.actorLoop pb)
        let psc = spawn m.Context ActorNames.ProjectSnapshotCreator.Name (ProjectSnapshotCreator.actorLoop pff)
        let rec loop() = 
            actor {
                let! msg = m.Receive()
                match msg with
                | ReadyForBuildProject(id, p) -> (id, p) |> CreateProjectSnapshot |> psc.Tell
                return! loop()
            }
        loop()

module ProjectBuildCoordinator = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) = 
        m.Context.System.EventStream.Subscribe<ProjectBuildCoordinatorMessage>(m.Self) |> ignore

        let opts = [ SpawnOption.Router(RoundRobinPool(Environment.ProcessorCount)) ]
        let pbsf = spawnOpt m.Context ActorNames.ProjectBuildStepsFactory.Name ProjectBuildStepsFactory.actorLoop opts
        let rec loop() = 
            actor {
                let! msg = m.Receive()
                match msg with
                | ReadyForBuildProject(id, p) -> (id, p) |> ReadyForBuildProject |> pbsf.Tell
                return! loop()
            }
        loop()

