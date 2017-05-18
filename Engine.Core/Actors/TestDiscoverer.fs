namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open R4nd0mApps.TddStud10.Common.Domain

module AssemblyTestsDiscoverer = 
    open ActorMessages

    let discoverAssemblyTestsWorker rid a (es: EventStream) (self : IActorRef) =
        async {
            let f tid =
                tid |> ReadyForRunTest |> es.Publish
                tid |> EvTestDiscovered |> es.Publish   

            let! res = Async.Catch <| API.discoverAssemblyTests f rid a
            match res with
            | Choice1Of2 r -> 
                (rid, a) |> DiscoverAssemblyTestsSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, a, fi) |> DiscoverAssemblyTestsFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTests(rid, a) -> 
                    (rid, a) |> EvAssemblyTestDiscoveryStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (discoverAssemblyTestsWorker rid a m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTestsSucceeded(rid, a) ->
                    (rid, a) |> EvAssemblyTestDiscoverySucceeded |> m.Context.System.EventStream.Publish
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
                | ReadyForDiscoverAssemblyTests(id, a) -> 
                    (id, a) |> DiscoverAssemblyTests |> td.Tell
                return! loop()
            }
        loop()
