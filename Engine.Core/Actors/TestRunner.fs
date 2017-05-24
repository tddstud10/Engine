namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open Akka.Event
open Akka.FSharp
open Akka.Routing
open System
open TestData

module TestRunner = 
    open ActorMessages

    let runTestErrorHandler rid t _ (self : IActorRef) =
        async {
            let! res = Async.Catch <| API.runTest rid t
            match res with
            | Choice1Of2 r -> 
                (rid, r) |> RunTestSucceeded |> self.Tell
            | Choice2Of2 e ->
                let fi = e |> FailureInfo.FromException
                (rid, t, fi) |> RunTestFailed |> self.Tell
        } |> Async.RunSynchronously
    
    let actorLoop (m : Actor<_>) =         
        let bgWorker = BackgroundWorker()
        m.Defer bgWorker.AbortBackgroundWorker

        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | RunTest(rid, t) -> 
                    (rid, t) |> EvTestRunStarting |> m.Context.System.EventStream.Publish
                    bgWorker.StartOnBackgroundWorker (runTestErrorHandler rid t m.Context.System.EventStream) m.Self 
                    return! loopTaskRunning()
                | _ -> Prelude.undefined
            }
        and loopTaskRunning() =
            actor {
                let! msg = m.Receive()
                match msg with
                | RunTestSucceeded(rid, r) ->
                    (rid, r) |> EvTestRunSucceeded |> m.Context.System.EventStream.Publish
                    m.UnstashAll()
                    return! loop()
                | RunTestFailed(rid, t, e) ->
                    (rid, t, e) |> EvTestRunFailed |> m.Context.System.EventStream.Publish
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

        let opts = [ SpawnOption.Router(RoundRobinPool(Environment.ProcessorCount)) ]
        let tr = spawnOpt m.Context ActorNames.TestRunner.Name TestRunner.actorLoop opts
        let rec loop() = 
            actor { 
                let! msg = m.Receive()
                match msg with
                | ReadyForRunTest(id, t) -> 
                    (id, t) |> RunTest |> tr.Tell
                return! loop()
            }
        loop()
