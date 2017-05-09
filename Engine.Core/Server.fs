module R4nd0mApps.TddStud10.Engine.Server

open FSharp.Data
open Microsoft.FSharp.Reflection
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.Logger
open System
open System.Reflection
open System.Runtime.Serialization

let logger = LoggerFactory.logger

type Serializer = obj -> string

module UrlSubPaths = 
    let ServerVersion = "/server/version"
    let ServerEvents = "/server/events"
    let Run = "/run"
    let RunState = "/run/state"
    let EngineState = "/engine/state"
    let DataStoreRunStartParams = "/datastore/runstartparams"
    let DataStoreSerializedState = "/datastore/serializedstate"
    let DataStoreFailureInfo = "/datastore/failureinfo"
    let DataStoreTests = "/datastore/tests"
    let DataStoreSequencePoints = "/datastore/sequencepoints"
    let DataStoreTestResultsForSequencePointIds = "/datastore/testresultsforsequencepointids"

type Version = 
    { Value : string }

type EngineInfo = 
    { Enabled : bool }

type RunInfo = 
    { InProgress : bool }

[<KnownType("KnownTypes")>]
type Notification = 
    | RunStateChanged of RunState
    | RunStarting of RunStartParams
    | RunStepStarting of RunStepStartingEventArg
    | RunStepError of RunStepErrorEventArg
    | RunStepEnded of RunStepEndedEventArg
    | RunError of RunFailureInfo
    | RunEnded of RunStartParams
    | TestCasesUpdated of unit
    | SequencePointsUpdated of unit
    | TestResultsUpdated of unit
    | TestFailureInfoUpdated of unit
    | CoverageInfoUpdated of unit
    static member KnownTypes() = 
        typeof<Notification>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
        |> Array.filter FSharpType.IsUnion

type Response<'T> = 
    { Kind : string
      Data : 'T }

let private callServer<'T> baseUrl p (o : HttpRequestBody option) = 
    logger.logInfof "|PROXY ACCESS| =====> %s %s %O" baseUrl p o
    Http.AsyncRequestString
        (sprintf "%s%s" baseUrl p, headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ], ?body = o) 
    |> Async.map (JsonConvert.deserialize<string []>
                  >> Seq.head
                  >> JsonConvert.deserialize<Response<'T>>
                  >> (fun a -> a.Data))

let getFromServer<'T> baseUrl p = None |> callServer<'T> baseUrl p

let postToServer<'T> baseUrl p = 
    JsonConvert.serialize
    >> TextRequest
    >> Some
    >> callServer<'T> baseUrl p

let createEventOfNotification<'T> eventSel (o : IObservable<Notification>) = 
    let e = Event<'T>()
    
    let sub = 
        o
        |> Observable.choose eventSel
        |> Observable.subscribe (e.Trigger)
    e, sub

module Notifications = 
    open System.Reactive.Linq
    open WebSocket4Net
    
    let createObservable (ws : WebSocket) = 
        Observable.Create<_>(fun (o : IObserver<_>) -> 
            ws.MessageReceived.AddHandler(fun _ ea -> 
                o.OnNext(ea.Message))
            ws.Closed.AddHandler(fun _ _ -> 
                logger.logInfof "WEBSOCKET CLIENT: Unsubscribing Observable."
                o.OnCompleted())
            ws.Error.AddHandler(fun _ ea -> 
                logger.logErrorf "WEBSOCKET CLIENT: Exception. %O." ea.Exception
                o.OnError(ea.Exception))
            ws.Opened.AddHandler(fun _ _ -> logger.logInfof "WEBSOCKET CLIENT: Subscribing Observable.")
            Action(ignore))
        |> Observable.map JsonConvert.deserialize<Notification>
