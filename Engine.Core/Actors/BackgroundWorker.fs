namespace R4nd0mApps.TddStud10.Engine.Actors

open Akka.Actor
open System.Threading

// NOTE: Switch to threadpool threads if this becomes too 'expensive'.
type BackgroundWorker() = 
    let workerThread : Thread ref = ref null
    
    member __.StartOnBackgroundWorker f (recipient : IActorRef) = 
        let threadFun () =
            Thread.CurrentThread.IsBackground <- true
            recipient |> f
        workerThread.Value <- Thread(threadFun)
        workerThread.Value.Start()
    
    member __.AbortBackgroundWorker() = 
        if not <| isNull workerThread.Value && workerThread.Value.IsAlive then 
            System.Console.WriteLine("*******> Aborting background worker")
            workerThread.Value.Abort()
