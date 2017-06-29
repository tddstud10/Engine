namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open ActorMessages

module AssemblySequencePointsDiscoverer = 
    let discoverAssemblySequencePointsWorker rid pbo (es: EventStream) (self : IActorRef) =
        async {
            let f x = 
                x |> EvSequencePointsDiscovered |> es.Publish   
                x |> DsSequencePointsDiscovered |> es.Publish   

            let! res = Async.Catch <| API.discoverAssemblySequencePoints f rid pbo
            match res with
            | Choice1Of2 r -> 
                (rid, pbo) |> DiscoverAssemblySequencePointsSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, pbo, fi) |> DiscoverAssemblySequencePointsFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblySequencePoints(rid, pbo) -> 
                    (rid, pbo) |> EvAssemblySequencePointsDiscoveryStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (discoverAssemblySequencePointsWorker rid pbo m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblySequencePointsSucceeded(rid, pbo) ->
                    (rid, pbo) |> EvAssemblySequencePointsDiscoverySucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | DiscoverAssemblySequencePointsFailed(rid, pbo, e) ->
                    (rid, pbo, e) |> EvAssemblySequencePointsDiscoveryFailed |> m.Context.System.EventStream.Publish
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
                | ReadyForDiscoverAssemblySequencePoints(id, pbo) -> 
                    (id, pbo) |> DiscoverAssemblySequencePoints |> td.Tell
                return! loop()
            }
        loop()
