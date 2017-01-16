namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine
open R4nd0mApps.TddStud10.Engine.Server
open R4nd0mApps.TddStud10.Logger
open System
open System.Reflection
open System.Threading
open System.Threading.Tasks

[<CLIMutable>]
type EngineParams = 
    { HostVersion : HostVersion
      EngineConfig : EngineConfig
      SolutionPath : FilePath
      SessionStartTime : DateTime }

type IEngineEvents = 
    inherit IDisposable
    abstract RunStateChanged : IEvent<RunState>
    abstract RunStarting : IEvent<RunStartParams>
    abstract RunStepStarting : IEvent<RunStepStartingEventArg>
    abstract RunStepError : IEvent<RunStepErrorEventArg>
    abstract RunStepEnded : IEvent<RunStepEndedEventArg>
    abstract RunError : IEvent<RunFailureInfo>
    abstract RunEnded : IEvent<RunStartParams>

type IEngineCallback = 
    abstract OnRunStateChanged : rs:RunState -> unit
    abstract OnRunStarting : rsp:RunStartParams -> unit
    abstract OnRunStepStarting : rssea:RunStepStartingEventArg -> unit
    abstract OnRunStepError : rseea:RunStepErrorEventArg -> unit
    abstract OnRunStepEnded : rseea:RunStepEndedEventArg -> unit
    abstract OnRunError : rfi:RunFailureInfo -> unit
    abstract OnRunEnded : rsp:RunStartParams -> unit

type EngineEventsLocal() = 
    let runStateChanged = new Event<_>()
    let runStarting = new Event<_>()
    let runStepStarting = new Event<_>()
    let runStepError = new Event<_>()
    let runStepEnded = new Event<_>()
    let runError = new Event<_>()
    let runEnded = new Event<_>()
    
    interface IEngineEvents with
        member __.RunStateChanged = runStateChanged.Publish
        member __.RunStarting = runStarting.Publish
        member __.RunStepStarting = runStepStarting.Publish
        member __.RunStepError = runStepError.Publish
        member __.RunStepEnded = runStepEnded.Publish
        member __.RunError = runError.Publish
        member __.RunEnded = runEnded.Publish
    
    interface IEngineCallback with
        member __.OnRunStateChanged(rs) = runStateChanged.Trigger(rs)
        member __.OnRunStarting(rsp) = runStarting.Trigger(rsp)
        member __.OnRunStepStarting(rssea) = runStepStarting.Trigger(rssea)
        member __.OnRunStepError(rseea) = runStepError.Trigger(rseea)
        member __.OnRunStepEnded(rseea) = runStepEnded.Trigger(rseea)
        member __.OnRunError(e) = runError.Trigger(e)
        member __.OnRunEnded(rsp) = runEnded.Trigger(rsp)

    interface IDisposable with
        member __.Dispose() = ()

type EngineEventsSource(notify) = 
    let disposed : bool ref = ref false
    let rsc = Event<_>()
    let rscSub = rsc.Publish |> Observable.subscribe (RunStateChanged >> notify)
    let rs = Event<_>()
    let rsSub = rs.Publish |> Observable.subscribe (RunStarting >> notify)
    let rss = Event<_>()
    let rssSub = rss.Publish |> Observable.subscribe (RunStepStarting >> notify)
    let rserr = Event<_>()
    let rserrSub = rserr.Publish |> Observable.subscribe (RunStepError >> notify)
    let rse = Event<_>()
    let rseSub = rse.Publish |> Observable.subscribe (RunStepEnded >> notify)
    let rerr = Event<_>()
    let rerrSub = rerr.Publish |> Observable.subscribe (RunError >> notify)
    let re = Event<_>()
    let reSub = re.Publish |> Observable.subscribe (RunEnded >> notify)
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                rscSub.Dispose()
                rsSub.Dispose()
                rssSub.Dispose()
                rserrSub.Dispose()
                rseSub.Dispose()
                rerrSub.Dispose()
                reSub.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IEngineCallback with
        member __.OnRunStateChanged(rs) = rsc.Trigger(rs)
        member __.OnRunStarting(rsp) = rs.Trigger(rsp)
        member __.OnRunStepStarting(rssea) = rss.Trigger(rssea)
        member __.OnRunStepError(rseea) = rserr.Trigger(rseea)
        member __.OnRunStepEnded(rseea) = rse.Trigger(rseea)
        member __.OnRunError(e) = rerr.Trigger(e)
        member __.OnRunEnded(rsp) = re.Trigger(rsp)

type EngineEventsSink(o) = 
    let disposed : bool ref = ref false
    
    let rsc, rscSub = 
        Server.createEventOfNotification<_> (function 
            | RunStateChanged x -> Some x
            | _ -> None) o
    
    let rs, rsSub = 
        Server.createEventOfNotification<_> (function 
            | RunStarting x -> Some x
            | _ -> None) o
    
    let rss, rssSub = 
        Server.createEventOfNotification<_> (function 
            | RunStepStarting x -> Some x
            | _ -> None) o
    
    let rserr, rserrSub = 
        Server.createEventOfNotification<_> (function 
            | RunStepError x -> Some x
            | _ -> None) o
    
    let rse, rseSub = 
        Server.createEventOfNotification<_> (function 
            | RunStepEnded x -> Some x
            | _ -> None) o
    
    let rerr, rerrSub = 
        Server.createEventOfNotification<_> (function 
            | RunError x -> Some x
            | _ -> None) o
    
    let re, reSub = 
        Server.createEventOfNotification<_> (function 
            | RunEnded x -> Some x
            | _ -> None) o
    
    abstract Dispose : bool -> unit
    
    override __.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                rscSub.Dispose()
                rsSub.Dispose()
                rssSub.Dispose()
                rserrSub.Dispose()
                rseSub.Dispose()
                rerrSub.Dispose()
                reSub.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
    
    interface IEngineEvents with
        member __.RunEnded = re.Publish
        member __.RunError = rerr.Publish
        member __.RunStarting = rs.Publish
        member __.RunStateChanged = rsc.Publish
        member __.RunStepEnded = rse.Publish
        member __.RunStepError = rserr.Publish
        member __.RunStepStarting = rss.Publish

type IEngine = 
    abstract Connect : unit -> unit
    abstract Disconnect : unit -> unit
    abstract DisableEngine : unit -> Async<bool>
    abstract IsEnabled : unit -> Async<bool>
    abstract EnableEngine : unit -> Async<bool>
    abstract RunEngine : ep:EngineParams -> Async<bool>
    abstract IsRunInProgress : unit -> Async<bool>

type Engine(dataStore : IDataStore, cb : IEngineCallback option) as x = 
    let logger = LoggerFactory.logger
    let engineParams : EngineParams option ref = ref None
    let cbs : IEngineCallback list ref = ref (cb |> Option.fold (fun _ e -> [ e ]) [])
    let invokeCbs f = !cbs |> List.iter (fun cb -> Exec.safeExec (fun () -> f cb))
    
    let createRunStepsTempHack() = 
        [ "R4nd0mApps.TddStud10.Engine"; "R4nd0mApps.TddStud10.Engine.DF" ]
        |> List.pick (fun an -> 
               let a = Assembly.Load(an)
               if obj.ReferenceEquals(a, null) then None
               else Some a)
        |> fun a -> a.GetType("R4nd0mApps.TddStud10.Engine.Engine")
        |> fun t -> t.GetMethod("CreateRunSteps", BindingFlags.Public ||| BindingFlags.Static)
        |> fun m -> m.Invoke(null, [| Func<_, _>(dataStore.FindTest) |]) :?> RunSteps
    
    let runner = TddStud10Runner.Create x (createRunStepsTempHack())
    let currentRun : Task<_> ref = ref (null :> _)
    let currentRunCts : CancellationTokenSource ref = ref (null :> _)
    let isEnabled : bool ref = ref false
    
    interface IEngine with
        
        member __.Connect() = 
            logger.logInfof "|ENGINE ACCESS| =====> Connect"
            let rsc = fun ea -> 
                (invokeCbs (fun cb -> cb.OnRunStateChanged(ea)))
            
            let rs = 
                fun ea -> 
                    dataStore.SetRunStartParams(ea)
                    invokeCbs (fun cb -> cb.OnRunStarting(ea))
            
            let rss = fun ea -> (invokeCbs (fun cb -> cb.OnRunStepStarting(ea)))
            let rser = fun ea -> (invokeCbs (fun cb -> cb.OnRunStepError(ea)))
            
            let rse = 
                fun ea -> 
                    dataStore.UpdateData(ea.rsr.runData)
                    invokeCbs (fun cb -> cb.OnRunStepEnded(ea))
            
            let rer = fun ea -> (invokeCbs (fun cb -> cb.OnRunError(ea)))
            let re = fun ea -> (invokeCbs (fun cb -> cb.OnRunEnded(ea)))
            do runner.AttachHandlers rsc rs rss rser rse rer re
        
        member __.EnableEngine() = 
            async { 
                logger.logInfof "|ENGINE ACCESS| =====> EnableEngine"
                isEnabled := true
                return !isEnabled
            }
        
        member __.IsEnabled() = 
            async { 
                logger.logInfof "|ENGINE ACCESS| =====> IsEnabled"
                return !isEnabled
            }
        
        member __.DisableEngine() = 
            async { 
                logger.logInfof "|ENGINE ACCESS| =====> DisableEngine"
                isEnabled := false
                dataStore.ResetData()
                return !isEnabled
            }
        
        member __.Disconnect() = 
            logger.logInfof "|ENGINE ACCESS| =====> Disconnect"
            runner.DetachHandlers()
        
        member __.IsRunInProgress() = 
            async { 
                logger.logInfof "|ENGINE ACCESS| =====> IsRunInProgress"
                return not 
                           (currentRun.Value = null 
                            || (currentRun.Value.Status = TaskStatus.Canceled 
                                || currentRun.Value.Status = TaskStatus.Faulted 
                                || currentRun.Value.Status = TaskStatus.RanToCompletion))
            }
        
        member x.RunEngine ep = 
            async { 
                engineParams := Some ep
                logger.logInfof "|ENGINE ACCESS| =====> RunEngine with EngineParams: %A" engineParams
                try 
                    if not isEnabled.Value then logger.logInfof "Cannot start engine. Host has denied request."
                    else 
                        let! isProgress = (x :> IEngine).IsRunInProgress()
                        if isProgress then logger.logInfof "Cannot start engine. A run is already in progress."
                        else 
                            logger.logInfof "Engine: Going to trigger a run."
                            // NOTE: Note fix the CT design once we wire up.
                            if (currentRunCts.Value <> null) then currentRunCts.Value.Dispose()
                            currentRunCts := new CancellationTokenSource()
                            currentRun 
                            := runner.StartAsync ep.EngineConfig ep.SessionStartTime ep.SolutionPath 
                                   currentRunCts.Value.Token
                with e -> logger.logErrorf "Exception thrown in InvokeEngine: %O." e
                return! (x :> IEngine).IsRunInProgress()
            }
    
    interface IRunExecutorHost with
        member __.CanContinue() = !isEnabled
        
        member __.HostVersion = 
            match !engineParams with
            | Some ep -> ep.HostVersion
            | _ -> failwithf "IRunExecutorHost called outside the context of a run!"
        
        member __.RunStateChanged(_) = ()

type EngineProxy(baseUrl) = 
    interface IEngine with
        member __.Connect() = ()
        member __.Disconnect() = ()
        member __.EnableEngine() = 
            Server.postToServer<Server.EngineInfo> baseUrl Server.UrlSubPaths.EngineState 
                { Server.EngineInfo.Enabled = true } |> Async.map (fun it -> it.Enabled)
        member __.DisableEngine() = 
            Server.postToServer<Server.EngineInfo> baseUrl Server.UrlSubPaths.EngineState 
                { Server.EngineInfo.Enabled = false } |> Async.map (fun it -> it.Enabled)
        member __.IsEnabled() = 
            Server.getFromServer<Server.EngineInfo> baseUrl Server.UrlSubPaths.EngineState 
            |> Async.map (fun it -> it.Enabled)
        member __.IsRunInProgress() = 
            Server.getFromServer<Server.RunInfo> baseUrl Server.UrlSubPaths.RunState 
            |> Async.map (fun it -> it.InProgress)
        member __.RunEngine(ep) = 
            Server.postToServer<Server.RunInfo> baseUrl Server.UrlSubPaths.Run ep |> Async.map (fun it -> it.InProgress)
