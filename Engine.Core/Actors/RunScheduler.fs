module R4nd0mApps.TddStud10.Engine.Actors.RunScheduler

open ActorMessages
open Akka.Event
open Akka.FSharp
open Hekate
open R4nd0mApps.TddStud10.Engine.Core

let actorLoop (m : Actor<_>) = 
    m.Context.System.EventStream.Subscribe<RunSchedulerMessage>(m.Self) |> ignore
    let opts = []
    spawnOpt m.Context ActorNames.ProjectBuildCoordinator.Name ProjectBuildCoordinator.actorLoop opts |> ignore
    let aspdc = 
        spawnOpt m.Context ActorNames.AssemblySequencePointsDiscovererCoordinator.Name 
            AssemblySequencePointsDiscovererCoordinator.actorLoop opts
    let atdc = 
        spawnOpt m.Context ActorNames.AssemblyTestsDiscovererCoordinator.Name 
            AssemblyTestsDiscovererCoordinator.actorLoop opts
    let trc = spawnOpt m.Context ActorNames.TestRunnerCoordinator.Name TestRunnerCoordinator.actorLoop opts
    
    let rec loop depGraph inProgress = 
        actor { 
            let! msg = m.Receive()
            match msg with
            | ScheduleProjectBuild(rsp, s) -> 
                (rsp, s.Projects)
                |> EvProjectsDiscovered
                |> m.Context.System.EventStream.Publish
                let dg = s |> SolutionLoader.createDGraph
                let roots = Graph.Nodes.toList dg |> List.filter (fun (n, _) -> Graph.Nodes.outwardDegree n dg = Some 0)
                roots |> List.iter (fst
                                    >> Prelude.tuple2 rsp
                                    >> ReadyForBuildProject
                                    >> m.Context.System.EventStream.Publish)
                return! loop dg inProgress
            | ScheduleProjectBuildSucceeded(rsp, p) -> 
                let dg = Graph.Nodes.remove p depGraph
                
                let roots = 
                    Graph.Nodes.toList dg
                    |> List.filter (fun (n, _) -> Graph.Nodes.outwardDegree n dg = Some 0)
                    |> List.filter (fun (n, _) -> not <| Set.contains n inProgress)
                roots |> List.iter (fst
                                    >> Prelude.tuple2 rsp
                                    >> ReadyForBuildProject
                                    >> m.Context.System.EventStream.Publish)
                let inProgress = roots |> List.fold (fun acc (n, _) -> Set.add n acc) inProgress
                return! loop dg inProgress
            | ScheduleProjectBuildFailed(_) -> 
                aspdc |> m.Context.Stop
                atdc |> m.Context.Stop
                trc |> m.Context.Stop
                return! loop depGraph inProgress
        }
    loop (Graph.create [] []) Set.empty
