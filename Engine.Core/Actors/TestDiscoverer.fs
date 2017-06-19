namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open ActorMessages
open R4nd0mApps.TddStud10.TestHost

module AssemblyTestsDiscoverer = 
    let discoverAssemblyTestsWorker svc (svcCB: TestAdapterServiceFactory.TestAdapterServiceCallback) rsp pbo (es: EventStream) (self : IActorRef) =
        async {
            let f x =
                x |> ReadyForRunTest |> es.Publish
                x |> EvTestDiscovered |> es.Publish   

            svcCB.Callback <- Prelude.tuple2 rsp >> f
            let! res = Async.Catch <| API.discoverAssemblyTests svc rsp pbo
            match res with
            | Choice1Of2 r -> 
                (rsp, r) |> DiscoverAssemblyTestsSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rsp, pbo, fi) |> DiscoverAssemblyTestsFailed |> self.Tell
        } |> Async.RunSynchronously

    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        let svcCB = TestAdapterServiceFactory.TestAdapterServiceCallback()
        let proc, svc = TestAdapterServiceFactory.create svcCB
        
        let cleanup () =
            bgWorker.AbortBackgroundWorker()
            proc.Dispose()

        m.Defer cleanup

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTests(rsp, pbo) -> 
                    (rsp, pbo) |> EvAssemblyTestDiscoveryStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (discoverAssemblyTestsWorker svc svcCB rsp pbo m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | DiscoverAssemblyTestsSucceeded(rsp, pbo) ->
                    (rsp, pbo) |> EvAssemblyTestDiscoverySucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | DiscoverAssemblyTestsFailed(rsp, a, e) ->
                    (rsp, a, e) |> EvAssemblyTestDiscoveryFailed |> m.Context.System.EventStream.Publish
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

        let opts = [ SpawnOption.Router(RoundRobinPool(2)) ]
        let td = spawnOpt m.Context ActorNames.AssemblyTestsDiscoverer.Name AssemblyTestsDiscoverer.actorLoop opts
        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | ReadyForDiscoverAssemblyTests(rsp, pbo) -> 
                    (rsp, pbo) |> DiscoverAssemblyTests |> td.Tell
                return! loop()
            }
        loop()
