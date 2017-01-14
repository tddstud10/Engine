namespace R4nd0mApps.TddStud10.Engine.Core

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine
open R4nd0mApps.TddStud10.Logger
open System.Diagnostics
open System.IO
open System.Reflection

type ITddStud10Host = 
    abstract Start : unit -> Async<string>
    abstract Stop : unit -> unit
    abstract GetVersion : unit -> Async<string>
    abstract IsRunning : bool
    abstract GetEngine : unit -> IEngine
    abstract GetEngineEvents : unit -> IEngineEvents
    abstract GetDataStore : unit -> IXDataStore
    abstract GetDataStoreEvents : unit -> IXDataStoreEvents

type TddStud10HostProxy(port : int, ?local0 : bool) = 
    let local = defaultArg local0 false
    let logger = LoggerFactory.logger
    let serverProcess : Process ref = ref null
    let baseUrl = sprintf "http://127.0.0.1:%d" port
    let ebaseUrl = sprintf "ws://127.0.0.1:%d%s" port
    let eventsWs = new WebSocket4Net.WebSocket(ebaseUrl Server.UrlSubPaths.ServerEvents)

    let (engineProxy : IEngine, engineEvents : IEngineEvents, dataStoreProxy : IXDataStore, 
         dataStoreEvents : IXDataStoreEvents) = 
        if local then 
            let ds = DataStore() :> IDataStore
            let ee = EngineEventsLocal()
            let e = Engine(ds, ee :> IEngineCallback |> Some) :> IEngine
            e.Connect()
            let xde = XDataStoreEventsLocal()
            let xds = XDataStore(ds, xde :> IXDataStoreCallback |> Some) :> IXDataStore
            e, ee :> _, xds, xde :> _
        else 
            let eo = Server.WebSocket.createObservable eventsWs
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
            logger.logInfof "Starting TddStud10 Server: \"%s\" %s" sp sa
            let proc = new Process()
            proc.StartInfo.FileName <- sp
            proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
            proc.StartInfo.Arguments <- sa
            proc.Start() |> ignore
            logger.logInfof "Started TddStud10 Server with PID: %d" proc.Id
            eventsWs.Open()
            proc
    
    let stopServer (proc : Process) = 
        if local then ()
        else 
            let innerFn() = 
                logger.logInfof "Stopping TddStud10 server..."
                if proc <> null then 
                    if proc.HasExited then logger.logInfof "TddStud10 has already exited. Exit code: %d" proc.Id
                    else 
                        logger.logInfof "TddStud10 is running currently. Killing it."
                        Exec.safeExec proc.Kill
                else logger.logInfof "TddStud10 was not started"
            Exec.safeExec innerFn
    
    interface ITddStud10Host with
        
        member i.Start() = 
            serverProcess := startServer()
            (i :> ITddStud10Host).GetVersion() |> Async.map (fun sVer -> 
                                                      let cVer = App.getVersion()
                                                      logger.logInfof "Server Version: %s, Client Version: %s" sVer cVer
                                                      if sVer |> String.equalsOrdinalCI cVer then sVer
                                                      else failwithf "Version mismatch")
        
        member __.Stop() = 
            stopServer !serverProcess
            serverProcess := null
        
        member __.GetVersion() = 
            if local then App.getVersion() |> Async.result
            else 
                Server.getFromServer<Server.Version> baseUrl Server.UrlSubPaths.ServerVersion 
                |> Async.map (fun it -> it.Value)
        
        member __.IsRunning = !serverProcess <> null && not (!serverProcess).HasExited
        member __.GetEngine() = engineProxy
        member __.GetEngineEvents() = engineEvents
        member __.GetDataStore() = dataStoreProxy
        member __.GetDataStoreEvents() = dataStoreEvents
