module R4nd0mApps.TddStud10.Engine.Server

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.Engine.Server
open R4nd0mApps.TddStud10.Logger
open Suave
open Suave.Filters
open Suave.Http
open Suave.Operators
open Suave.Sockets.Control
open Suave.Web
open Suave.WebPart
open Suave.WebSocket
open System.Diagnostics
open System.Threading
open System

(* 
   TODO: PARTHO: 
   - Split out the Engine and DataStore interactions into their individual classes/sockets/agents/etc.
   - Break out code units into their individual files
*)

let logger = LoggerFactory.logger

type SocketSenderMessages = 
    | Initialize of WebSocket
    | PushNotification of Notification
    | OpCodePong

let sockSender = 
    MailboxProcessor.Start(fun inbox -> 
        logger.logInfof "SOCK SENDER AGENT: Starting"
        let rec loop (ws : WebSocket option) = 
            async { 
                let! msg = inbox.Receive()
                match ws, msg with
                | _, Initialize ws -> 
                    logger.logInfof "SOCK SENDER AGENT: Initializing"
                    return! loop (Some ws)
                | None, msg -> 
                    logger.logWarnf "SOCK SENDER AGENT: Ignoring message %A as we are not initialized" msg
                    return! loop None
                | Some ws, OpCodePong -> let! _ = ws.send Pong (ArraySegment([||])) true
                                         return! loop (Some ws)
                | Some ws, PushNotification n -> 
                    logger.logInfof "SOCK SENDER AGENT: sending message. %O..." n
                    let bs = n |> JsonConvert.serialize |> UTF8.bytes |> ArraySegment
                    let! _ = ws.send Text bs true
                    return! loop (Some ws)
            }
        loop None)

sockSender.Error.Add(logger.logErrorf "SOCK SENDER AGENT: Errored out: %O")

let socketHandler (ws : WebSocket) _ = 
    ws
    |> Initialize
    |> sockSender.Post
    let rec socketFn() = 
        socket { 
            let! m = ws.read()
            match m with
            | Ping, _, _ -> 
                logger.logInfof "SOCK HANDLER: Received ping, replying with pong"
                OpCodePong |> sockSender.Post
                return! socketFn()
            | oc, _, _ -> 
                logger.logWarnf "SOCK HANDLER: Received unexpected message. Ignoring it. %A" oc
                return! socketFn()
        }
    socketFn()

let handler f : WebPart = 
    let getResourceFromReq (req : HttpRequest) = req.rawForm |> UTF8.toString
    fun (r : HttpContext) -> 
        async { 
            let data = 
                r.request
                |> getResourceFromReq
                |> JsonConvert.deserialize<'a>
            let! res = Async.Catch(f data)
            match res with
            | Choice1Of2 res -> 
                let res' = 
                    res
                    |> List.toArray
                    |> Json.toJson
                return! Response.response HttpCode.HTTP_200 res' r
            | Choice2Of2 e -> return! Response.response HttpCode.HTTP_500 (Json.toJson e) r
        }

let rec sucideOnParentExit ppid = 
    async { 
        let pRunning = 
            Exec.safeExec2 (fun () -> Process.GetProcessById(ppid)) |> Option.fold (fun _ e -> not e.HasExited) false
        if not pRunning then 
            logger.logInfof "Parent (ID = %d) is not running. Committing suicide..." ppid
            Process.GetCurrentProcess().Kill()
        do! Async.Sleep(60000)
        return! sucideOnParentExit ppid
    }

module CommandResponse = 
    let info (serialize : Serializer) (s : string) = 
        serialize { Kind = "info"
                    Data = s }
    
    let error (serialize : Serializer) (s : string) = 
        serialize { Kind = "error"
                    Data = s }
    
    let serverVersion (serialize : Serializer) () = 
        serialize { Kind = "serverVersion"
                    Data = { Version.Value = App.getVersion() } }
    
    let engineState (serialize : Serializer) es = 
        serialize { Kind = "engineState"
                    Data = { EngineInfo.Enabled = es } }
    
    let runState (serialize : Serializer) rs = 
        serialize { Kind = "runState"
                    Data = { RunInfo.InProgress = rs } }
    
    let dataStoreRespose (serialize : Serializer) kind (data) = 
        let x = 
            { Kind = kind
              Data = data }
        serialize x

type Commands(notify, serialize : Serializer) = 
    let dataStore = DataStore()
    let e = Engine(dataStore, new EngineEventsSource(notify) :> IEngineCallback |> Some) :> IEngine
    do e.Connect()
    let ds = XDataStore(dataStore, new XDataStoreEventsSource(notify) :> IXDataStoreCallback |> Some) :> IXDataStore
    member __.GetServerVersion() = [ CommandResponse.serverVersion serialize () ] |> async.Return
    member __.GetEngineState() = e.IsEnabled() |> Async.map (fun es -> [ CommandResponse.engineState serialize es ])
    
    member i.SetEngineState(es : EngineInfo) = 
        let set = 
            if es.Enabled then e.EnableEngine()
            else e.DisableEngine()
        set |> Async.bind (fun _ -> i.GetEngineState())
    
    member i.CreateRun(ep : EngineParams) = 
        ep
        |> e.RunEngine
        |> Async.bind (fun _ -> i.GetRunState())
    
    member __.GetRunState() = e.IsRunInProgress() |> Async.map (fun es -> [ CommandResponse.runState serialize es ])
    member __.GetDataStoreRunStartParams() = 
        ds.GetRunStartParams() 
        |> Async.map (fun es -> [ CommandResponse.dataStoreRespose serialize "runStartParams" es ])
    member __.GetDataStoreFailureInfo(fp) = 
        ds.GetTestFailureInfosInFile(fp) 
        |> Async.map (fun tfis -> [ CommandResponse.dataStoreRespose serialize "testFailureInfo" tfis ])
    member __.GetDataStoreTests(fp) = 
        ds.GetTestsInFile(fp) |> Async.map (fun ts -> [ CommandResponse.dataStoreRespose serialize "tests" ts ])
    member __.GetDataStoreSequencePoints(fp) = 
        ds.GetSequencePointsForFile(fp) 
        |> Async.map (fun sps -> [ CommandResponse.dataStoreRespose serialize "sequencePoints" sps ])
    member __.GetDataStoreTestResultsForSequencePoints(spids) = 
        ds.GetTestResultsForSequencepointsIds(spids) 
        |> Async.map (fun trs -> [ CommandResponse.dataStoreRespose serialize "testResultsForSequencePoints" trs ])
    member __.GetDataStoreSerializedState() = 
        ds.GetSerializedState() 
        |> Async.map (fun s -> [ CommandResponse.dataStoreRespose serialize "serializedState" s ])

// Test with: curl -Uri 'http://127.0.0.1:9999/server/version' -Method Get 
let createRoutes (commands : Commands) = 
    choose [ Filters.GET 
             >=> choose [ path UrlSubPaths.ServerEvents >=> handShake socketHandler
                          path UrlSubPaths.ServerVersion >=> handler commands.GetServerVersion
                          path UrlSubPaths.EngineState >=> handler commands.GetEngineState
                          path UrlSubPaths.RunState >=> handler commands.GetRunState
                          path UrlSubPaths.DataStoreRunStartParams >=> handler commands.GetDataStoreRunStartParams
                          path UrlSubPaths.DataStoreSerializedState >=> handler commands.GetDataStoreSerializedState ]
             
             Filters.POST 
             >=> choose 
                     [ path UrlSubPaths.EngineState >=> handler commands.SetEngineState
                       path UrlSubPaths.Run >=> handler commands.CreateRun
                       path UrlSubPaths.DataStoreFailureInfo >=> handler commands.GetDataStoreFailureInfo
                       path UrlSubPaths.DataStoreTests >=> handler commands.GetDataStoreTests
                       path UrlSubPaths.DataStoreSequencePoints >=> handler commands.GetDataStoreSequencePoints
                       
                       path UrlSubPaths.DataStoreTestResultsForSequencePointIds 
                       >=> handler commands.GetDataStoreTestResultsForSequencePoints ]
             RequestErrors.NOT_FOUND "Found no handlers." ]
    >=> Writers.setMimeType "application/json; charset=utf-8"

[<EntryPoint>]
let main (argv : string[]) = 
    let ppid, port = int argv.[0], int argv.[1]
    let commands = Commands(PushNotification >> sockSender.Post, JsonConvert.serialize)
    let app = createRoutes commands
    ThreadPool.SetMinThreads(8, 8) |> ignore
    ppid
    |> sucideOnParentExit
    |> Async.Start
    let defaultBinding = defaultConfig.bindings.[0]
    let withPort = { defaultBinding.socketBinding with port = uint16 port }
    let serverConfig = { defaultConfig with bindings = [ { defaultBinding with socketBinding = withPort } ] }
    startWebServer serverConfig app
    0

open Akka.Actor
open Akka.FSharp
open R4nd0mApps.TddStud10.Engine.Actors
open R4nd0mApps.TddStud10.Engine.Actors.ActorMessages
open R4nd0mApps.TddStud10.Common.Domain

(*

Scenarios:
v Resync
v Cancel
  v Resync
  v resync With hung operation
- Incr
- Cancel
  - Incr
  - Incr With hung operation

TODO:
- IDE integration
  - IDE Events 
    - Specifics of each step - names of projects discovered, etc.
  - Console log to actual logs
  - Redirect akka.net Logging to our logger
- Incremental
  - Test discover method will be different, will do lookup on files changed
  - Combined data since last resync
x Seperate executable for build and test
x Cooperative cancellation + assign cooperative cancellation
x Simplify messaging - ProjectBuildDone and BuildDone should ideally be the same

*)

(*[<EntryPoint>]
let main2 _ = 
    let system = System.create ActorNames.System.Name (Configuration.load())

    spawn system ActorNames.IdEvents.Name IdeEvents.actorLoop |> ignore
    spawn system ActorNames.DataStore.Name ADataStore.actorLoop |> ignore

    let runner = spawn system ActorNames.Runner.Name Runner.actorLoop

    let rsp =
        { Id = Guid.NewGuid()
          Solution = 
            { Path = @"D:\src\t\Engine\Engine.sln" |> FilePath
              SnapshotPath = "" |> FilePath
              BuildRoot = "" |> FilePath }
          Config = EngineConfigLoader.defaultValue<_> }

    rsp |> Resync |> runner.Tell

//    System.Threading.Thread.Sleep(2000)
//
//    CancelRun |> runner.Tell
//
//    System.Threading.Thread.Sleep(1000)
//
//    rsp |> Resync |> runner.Tell
//
//    System.Threading.Thread.Sleep(3000)
//
//    CancelRun |> runner.Tell
//
//    System.Threading.Thread.Sleep(1000)
//
//    rsp |> Resync |> runner.Tell

    system.WhenTerminated.Wait()

    0 // return an integer exit code
*)
