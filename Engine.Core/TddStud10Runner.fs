namespace R4nd0mApps.TddStud10.Engine.Core

open System
open R4nd0mApps.TddStud10.Common.Domain

(* NOTE: 
   This should just be a wire up class. No business logic at all. 
   Hence unit tests not required. *)
type public TddStud10Runner private (re, agent, rst) = 
    let rscSub : IDisposable ref = ref null
    let shSub : IDisposable ref = ref null
    let sshSub : IDisposable ref = ref null
    let serhSub : IDisposable ref = ref null
    let sehSub : IDisposable ref = ref null
    let erhSub : IDisposable ref = ref null
    let ehSub : IDisposable ref = ref null
    
    static member public CreateRunStep(info : RunStepInfo, 
                                       func : Func<IRunExecutorHost, RunStartParams, RunStepInfo, RunStepResult>) : RunStep = 
        { info = info
          func = fun h sp i _ -> func.Invoke(h, sp, i) }
    
    static member public Create host runSteps = 
        let all f = 
            f
            |> RunStepFuncBehaviors.eventsPublisher
            |> RunStepFuncBehaviors.stepLogger
            |> RunStepFuncBehaviors.stepTimer
        
        let re = RunExecutor.Create host runSteps all
        let agent = new StaleMessageIgnoringAgent<EngineConfig * DateTime * FilePath>(re.Start >> ignore)
        let rst = new RunStateTracker()
        re.RunStarting.Add(rst.OnRunStarting)
        re.RunStepStarting.Add(rst.OnRunStepStarting)
        re.OnRunStepError.Add(rst.OnRunStepError)
        re.RunStepEnded.Add(rst.OnRunStepEnd)
        re.OnRunError.Add(rst.OnRunError)
        re.RunEnded.Add(rst.OnRunEnd)
        new TddStud10Runner(re, agent, rst)
    
    member public __.AttachHandlers (rsc : RunState -> unit) (sh : RunStartParams -> unit) (ssh : RunStepStartingEventArg -> unit) 
           (serh : RunStepErrorEventArg -> unit) (seh : RunStepEndedEventArg -> unit) (erh : RunFailureInfo -> unit) 
           (eh : RunStartParams -> unit) = 
        rscSub := rst.RunStateChanged |> Observable.subscribe rsc
        shSub := re.RunStarting |> Observable.subscribe sh
        sshSub := re.RunStepStarting |> Observable.subscribe ssh
        serhSub := re.OnRunStepError |> Observable.subscribe serh
        sehSub := re.RunStepEnded |> Observable.subscribe seh
        erhSub := re.OnRunError |> Observable.subscribe (RunFailureInfo.FromExeption >> erh)
        ehSub := re.RunEnded |> Observable.subscribe eh

    member public __.DetachHandlers () = 
        if !ehSub <> null then ehSub.contents.Dispose()
        if !erhSub <> null then erhSub.contents.Dispose()
        if !sehSub <> null then sehSub.contents.Dispose()
        if !serhSub <> null then serhSub.contents.Dispose()
        if !sshSub <> null then sshSub.contents.Dispose()
        if !shSub <> null then shSub.contents.Dispose()
        if !rscSub <> null then rscSub.contents.Dispose()

    member public __.StartAsync cfg startTime slnPath token = agent.SendMessageAsync (cfg, startTime, slnPath) token
    member public __.StopAsync(token) = agent.StopAsync(token)
