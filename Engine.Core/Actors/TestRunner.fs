namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData
open R4nd0mApps.TddStud10.TestHost

module TestRunner = 
    open ActorMessages

    let runTestErrorHandler svc rsp t (es: EventStream) (self : IActorRef) =
        async {
            let f x =
                x |> DsTestRunSucceeded |> es.Publish   

            let! res = Async.Catch <| API.runTest svc rsp t f
            match res with
            | Choice1Of2 r -> 
                (rsp, r) |> RunTestSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rsp, t, fi) |> RunTestFailed |> self.Tell
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
                | RunTest(rsp, t) -> 
                    (rsp, t) |> EvTestRunStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (runTestErrorHandler svc rsp t m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | RunTestSucceeded(rsp, r) ->
                    (rsp, r) |> EvTestRunSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | RunTestFailed(rsp, t, e) ->
                    (rsp, t, e) |> EvTestRunFailed |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | _ -> 
                    m.Stash()
                    return! loopTaskRunning()
            }
        loop()

module TestRunnerCoordinator = 
    open ActorMessages
    
    let actorLoop (m : Actor<_>) =         
        m.Context.System.EventStream.Subscribe<TestRunnerCoordinatorMessage>(m.Self) |> ignore

        let opts = [ SpawnOption.Router(RoundRobinPool(2)) ]
        let tr = spawnOpt m.Context ActorNames.TestRunner.Name TestRunner.actorLoop opts
        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | ReadyForRunTest(rsp, t) -> 
                    (rsp, t) |> RunTest |> tr.Tell
                return! loop()
            }
        loop()
