namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open R4nd0mApps.TddStud10.Common.Domain

module Runner = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) = 
        let rec loop rs = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | Resync(id, s) -> 
                    id |> EvResyncStarting |> m.Context.System.EventStream.Publish
                    DsInitialize |> m.Context.System.EventStream.Publish
                    rs |> Option.iter m.Context.Stop
                    let rs = spawn m.Context ActorNames.RunScheduler.Name RunScheduler.actorLoop
                    (id, s) |> ScheduleProjectBuild |> rs.Tell
                    return! loop (Some rs)
                | CancelRun -> 
                    rs |> Option.iter m.Context.Stop
                    return! loop None
                | _ -> Prelude.undefined
            }
        loop None 
