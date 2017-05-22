namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open ActorMessages

module AssemblyTestsDiscoverer = 
    let discoverAssemblyTestsWorker rid pbo (es: EventStream) (self : IActorRef) =
        async {
            let f tid =
                tid |> ReadyForRunTest |> es.Publish
                tid |> EvTestDiscovered |> es.Publish   

            let! res = Async.Catch <| API.discoverAssemblyTests f rid pbo
            match res with
            | Choice1Of2 r -> 
                (rid, pbo) |> DiscoverAssemblyTestsSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, pbo, fi) |> DiscoverAssemblyTestsFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTests(rid, pbo) -> 
                    (rid, pbo) |> EvAssemblyTestDiscoveryStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (discoverAssemblyTestsWorker rid pbo m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTestsSucceeded(rid, pbo) ->
                    (rid, pbo) |> EvAssemblyTestDiscoverySucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | DiscoverAssemblyTestsFailed(rid, a, e) ->
                    (rid, a, e) |> EvAssemblyTestDiscoveryFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module AssemblyTestsDiscovererCoordinator = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) =         
        m.Context.System.EventStream.Subscribe<AssemblyTestsDiscovererCoordinatorMessage>(m.Self) |> ignore

        let opts = [ SpawnOption.Router(RoundRobinPool(Environment.ProcessorCount)) ]
        let td = spawnOpt m.Context ActorNames.AssemblyTestsDiscoverer.Name AssemblyTestsDiscoverer.actorLoop opts
        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | ReadyForDiscoverAssemblyTests(id, pbo) -> 
                    (id, pbo) |> DiscoverAssemblyTests |> td.Tell
                return! loop()
            }
        loop()
