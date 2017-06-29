module R4nd0mApps.TddStud10.Engine.Actors.Runner

open ActorMessages
open Akka.Actor
open Akka.FSharp
open R4nd0mApps.TddStud10.Engine.Core

let actorLoop (m : Actor<_>) = 
    let rec loop rs = 
        actor { 
            let! msg = m.Receive()
            match msg with
            | Resync rp -> 
                rp
                |> EvResyncStarting
                |> m.Context.System.EventStream.Publish
                rp |> DsInitialize |> m.Context.System.EventStream.Publish
                rs |> Option.iter m.Context.Stop
                let rs = spawn m.Context ActorNames.RunScheduler.Name RunScheduler.actorLoop
                let sln = rp.SolutionPaths.Path |> SolutionLoader.load rp.Config.SnapshotExcludePatterns
                (rp, sln)
                |> ScheduleProjectBuild
                |> rs.Tell
                return! loop (Some rs)
            | CancelRun -> 
                rs |> Option.iter m.Context.Stop
                return! loop None
            | _ -> Prelude.undefined
        }
    loop None
