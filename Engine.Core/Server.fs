module R4nd0mApps.TddStud10.Engine.Server

open FSharp.Data
open Newtonsoft.Json
open R4nd0mApps.TddStud10.Logger

let logger = LoggerFactory.logger

module UrlSubPaths = 
    let ServerVersion = "/server/version"
    let Run = "/run"
    let RunState = "/run/state"
    let EngineState = "/engine/state"
    let DataStoreSerializedState = "/datastore/serializedstate"
    let DataStoreFailureInfo = "/datastore/failureinfo"
    let DataStoreTests = "/datastore/tests"
    let DataStoreSequencePoints = "/datastore/sequencepoints"
    let DataStoreTestResultsForSequencePointIds = "/datastore/testresultsforsequencepointids"

type Version = 
    { Value : string }

type EngineState = 
    { Enabled : bool }

type RunState = 
    { InProgress : bool }

type Response<'T> = 
    { Kind : string
      Data : 'T }

let private callServer<'T> baseUrl p (o : HttpRequestBody option) = 
    logger.logInfof "|ENGINE ACCESS| =====> %s %s %O" baseUrl p o
    Http.AsyncRequestString
        (sprintf "%s%s" baseUrl p, headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ], ?body = o) 
    |> Async.map 
           (fun r -> 
           JsonConvert.DeserializeObject<string []>(r).[0] 
           |> fun r -> JsonConvert.DeserializeObject<Response<'T>>(r).Data)

let getFromServer<'T> baseUrl p = None |> callServer<'T> baseUrl p

let postToServer<'T> baseUrl p = 
    JsonConvert.SerializeObject
    >> TextRequest
    >> Some
    >> callServer<'T> baseUrl p
