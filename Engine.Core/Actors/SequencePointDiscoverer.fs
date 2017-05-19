namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData

module AssemblySequencePointsDiscoverer = 
    open ActorMessages

    let discoverAssemblySequencePointsWorker rid a (es: EventStream) (self : IActorRef) =
        async {
            let f = EvSequencePointsDiscovered >> es.Publish   

            let! res = Async.Catch <| API.discoverAssemblySequencePoints f rid a
            match res with
            | Choice1Of2 r -> 
                (rid, a) |> DiscoverAssemblySequencePointsSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, a, fi) |> DiscoverAssemblySequencePointsFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblySequencePoints(rid, a) -> 
                    (rid, a) |> EvAssemblySequencePointsDiscoveryStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (discoverAssemblySequencePointsWorker rid a m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblySequencePointsSucceeded(rid, a) ->
                    (rid, a) |> EvAssemblySequencePointsDiscoverySucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | DiscoverAssemblySequencePointsFailed(rid, a, e) ->
                    (rid, a, e) |> EvAssemblySequencePointsDiscoveryFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module AssemblySequencePointsDiscovererCoordinator = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) =         
        m.Context.System.EventStream.Subscribe<AssemblySequencePointsDiscovererCoordinatorMessage>(m.Self) |> ignore

        let opts = [ SpawnOption.Router(RoundRobinPool(Environment.ProcessorCount)) ]
        let td = spawnOpt m.Context ActorNames.AssemblySequencePointsDiscoverer.Name AssemblySequencePointsDiscoverer.actorLoop opts
        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | ReadyForDiscoverAssemblySequencePoints(id, a) -> 
                    (id, a) |> DiscoverAssemblySequencePoints |> td.Tell
                return! loop()
            }
        loop()
