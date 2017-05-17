namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine
open R4nd0mApps.TddStud10.Logger
open System
open System.Diagnostics
open System.IO
open System.Reflection

type ITddStud10Host =
    inherit IDisposable 
    abstract Start : unit -> Async<string>
    abstract Stop : unit -> unit
    abstract GetVersion : unit -> Async<string>
    abstract IsRunning : bool
    abstract GetEngine : unit -> IEngine
    abstract GetEngineEvents : unit -> IEngineEvents
    abstract GetDataStore : unit -> IXDataStore
    abstract GetDataStoreEvents : unit -> IXDataStoreEvents

type TddStud10HostProxy(port : int, ?local0 : bool) = 
    let disposed : bool ref = ref false
    let local = defaultArg local0 false
    let logger = LoggerFactory.logger
    let serverProcess : Process ref = ref null
    let baseUrl = sprintf "http://127.0.0.1:%d" port
    let ebaseUrl = sprintf "ws://127.0.0.1:%d%s" port
    let eventsWs = new WebSocket4Net.WebSocket(ebaseUrl Server.UrlSubPaths.ServerEvents)

    let (engine : IEngine, engineEvents : IEngineEvents, dataStore : IXDataStore, 
         dataStoreEvents : IXDataStoreEvents) = 
        if local then 
            let ds = DataStore() :> IDataStore
            let ee = new EngineEventsLocal()
            let e = Engine(ds, ee :> IEngineCallback |> Some) :> IEngine
            let xde = new XDataStoreEventsLocal()
            let xds = XDataStore(ds, xde :> IXDataStoreCallback |> Some) :> IXDataStore
            e, ee :> _, xds, xde :> _
        else 
            let eo = Server.Notifications.createObservable eventsWs
            EngineProxy(baseUrl) :> _, new EngineEventsSink(eo) :> _, XDataStoreProxy(baseUrl) :> _, 
            new XDataStoreEventsSink(eo) :> _
    
    let getServerExePath() = 
        let cd = Assembly.GetExecutingAssembly() |> Assembly.getAssemblyLocation
        [ "R4nd0mApps.TddStud10.Engine.Server.exe"; "R4nd0mApps.TddStud10.Engine.Server.DF.exe" ]
        |> List.map (fun exe -> Path.Combine(cd, exe))
        |> List.find File.Exists
    
    let startServer() = 
        if local then
            Process.GetCurrentProcess()
        else
            let sp = getServerExePath()
            let sa = sprintf "%d %d" (Process.GetCurrentProcess().Id) port
            logger.logInfof "TddStud10Host: Starting Server: \"%s\" %s" sp sa
            let proc = new Process()
            proc.StartInfo.FileName <- sp
            proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
            proc.StartInfo.Arguments <- sa
            proc.Start() |> ignore
            logger.logInfof "TddStud10Host: Started TddStud10 Server with PID: %d" proc.Id
            engine.Connect()
            eventsWs.Open()
            logger.logInfof "TddStud10Host: Events socket is now in %A state" eventsWs.State
            proc
    
    let stopServer (proc : Process) = 
        if local then ()
        else 
            let innerFn() = 
                eventsWs.Close()
                logger.logInfof "TddStud10Host: Events socket is now in %A state" eventsWs.State
                logger.logInfof "TddStud10Host: Stopping TddStud10 server..."
                if proc |> isNull |> not then 
                    if proc.HasExited then logger.logInfof "TddStud10Host: Server has already exited. Exit code: %d" proc.Id
                    else 
                        logger.logInfof "TddStud10Host: Serveris running currently. Killing it."
                        Exec.safeExec proc.Kill
                else logger.logInfof "TddStud10Host: Server was not started"
            Exec.safeExec innerFn
    
    interface ITddStud10Host with
        
        member i.Start() = 
            serverProcess := startServer()
            (i :> ITddStud10Host).GetVersion() |> Async.map (fun sVer -> 
                                                      let cVer = App.getVersion()
                                                      logger.logInfof "TddStud10Host: Server Version: %s, Client Version: %s" sVer cVer
                                                      if sVer |> String.equalsOrdinalCI cVer then sVer
                                                      else failwithf "TddStud10Host: Version mismatch")
        
        member __.Stop() = 
            stopServer !serverProcess
            serverProcess := null
        
        member __.GetVersion() = 
            if local then App.getVersion() |> Async.result
            else 
                Server.getFromServer<Server.Version> baseUrl Server.UrlSubPaths.ServerVersion 
                |> Async.map (fun it -> it.Value)
        
        member __.IsRunning = !serverProcess |> isNull |> not && not (!serverProcess).HasExited
        member __.GetEngine() = engine
        member __.GetEngineEvents() = engineEvents
        member __.GetDataStore() = dataStore
        member __.GetDataStoreEvents() = dataStoreEvents
    
    abstract Dispose : bool -> unit
    
    override i.Dispose(disposing) = 
        if not disposed.Value then 
            if disposing then 
                (i :> ITddStud10Host).Stop()
                eventsWs.Dispose()
                engine.Disconnect()
                dataStoreEvents.Dispose()
                engineEvents.Dispose()
            disposed := true
    
    interface IDisposable with
        member x.Dispose() = 
            x.Dispose(true)
            GC.SuppressFinalize(x)
