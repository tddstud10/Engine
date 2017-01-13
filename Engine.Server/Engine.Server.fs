module R4nd0mApps.TddStud10.Engine.Server

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.Engine.Server
open R4nd0mApps.TddStud10.Logger
open Suave
open Suave.Filters
open Suave.Http
open Suave.Operators
open Suave.Web
open Suave.WebPart
open System.Diagnostics
open System.Reflection
open System.Threading

let logger = LoggerFactory.logger

[<AutoOpen>]
module internal Utils = 
    open Newtonsoft.Json
    
    let private fromJson<'a> json = JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a
    
    let getResourceFromReq<'a> (req : HttpRequest) = 
        let getString rawForm = System.Text.Encoding.UTF8.GetString(rawForm)
        req.rawForm
        |> getString
        |> fromJson<'a>

let handler f : WebPart = 
    fun (r : HttpContext) -> 
        async { 
            let data = r.request |> getResourceFromReq
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

type Serializer = obj -> string

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
                    Data = { EngineState.Enabled = es } }
    
    let runState (serialize : Serializer) rs = 
        serialize { Kind = "runState"
                    Data = { RunState.InProgress = rs } }
    
    let dataStoreRespose (serialize : Serializer) kind data = 
        serialize { Kind = kind
                    Data = data }

module Response = CommandResponse

(*
type Commands(ns, serialize : Serializer) = 
    let dataStore = DataStore()
    let e = Engine(dataStore, new EngineEventsSource(ns) :> IEngineCallback |> Some) :> IEngine
    do e.Connect()
    let ds = XDataStore(dataStore, new XDataStoreEventsSource(ns) :> IXDataStoreCallback |> Some) :> IXDataStore
    member __.GetServerVersion() = [ Response.serverVersion serialize () ] |> async.Return
    member __.GetEngineState() = e.IsEnabled() |> Async.map (fun es -> [ Response.engineState serialize (es) ])
    
    member i.SetEngineState(es : EngineState) = 
        let set = 
            if es.Enabled then e.EnableEngine()
            else e.DisableEngine()
        set |> Async.bind (fun _ -> i.GetEngineState())
    
    member i.CreateRun(ep : EngineParams) = 
        ep
        |> e.RunEngine
        |> Async.bind (fun _ -> i.GetRunState())
    
    member __.GetRunState() = e.IsRunInProgress() |> Async.map (fun es -> [ Response.runState serialize es ])
    member __.GetDataStoreFailureInfo(fp) = 
        ds.GetTestFailureInfosInFile(fp) 
        |> Async.map (fun tfis -> [ Response.dataStoreRespose serialize "testFailureInfo" tfis ])
    member __.GetDataStoreTests(fp) = 
        ds.GetTestsInFile(fp) |> Async.map (fun ts -> [ Response.dataStoreRespose serialize "tests" ts ])
    member __.GetDataStoreSequencePoints(fp) = 
        ds.GetSequencePointsForFile(fp) 
        |> Async.map (fun sps -> [ Response.dataStoreRespose serialize "sequencePoints" sps ])
    member __.GetDataStoreTestResultsForSequencePoints(spids) = 
        ds.GetTestResultsForSequencepointsIds(spids) 
        |> Async.map (fun trs -> [ Response.dataStoreRespose serialize "testResultsForSequencePoints" trs ])
    member __.GetDataStoreSerializedState() = 
        ds.GetSerializedState() |> Async.map (fun s -> [ Response.dataStoreRespose serialize "serializedState" s ])

// Test with: curl -Uri 'http://127.0.0.1:9999/server/version' -Method Get 
let createRoutes (commands : Commands) = 
    choose 
        [ Filters.GET 
          >=> choose [ path UrlSubPaths.ServerVersion >=> handler commands.GetServerVersion
                       path UrlSubPaths.EngineState >=> handler commands.GetEngineState
                       path UrlSubPaths.RunState >=> handler commands.GetRunState
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
*)

[<EntryPoint>]
let main argv = 
    let ppid, port, ns = int argv.[0], int argv.[1], argv.[2]
    //let commands = Commands(ns, JsonSerializer.writeJson)
    let app = choose [ ] // createRoutes commands
    ThreadPool.SetMinThreads(8, 8) |> ignore
    ppid
    |> sucideOnParentExit
    |> Async.Start
    let defaultBinding = defaultConfig.bindings.[0]
    let withPort = { defaultBinding.socketBinding with port = uint16 port }
    let serverConfig = { defaultConfig with bindings = [ { defaultBinding with socketBinding = withPort } ] }
    startWebServer serverConfig app
    0
