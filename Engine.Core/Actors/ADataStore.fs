module R4nd0mApps.TddStud10.Engine.Actors.ADataStore

open ActorMessages
open Akka.Event
open Akka.FSharp
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.TestHost
open R4nd0mApps.XTestPlatform.Api
open SQLite
open System
open System.IO

[<CLIMutable>]
type DsSequencePoint = 
    { [<PrimaryKey>]
      Id : string
      [<Indexed>]
      Document : string
      StartLine : int
      StartColumn : int
      EndLine : int
      EndColumn : int }
    static member FromSequencePoint(sp : SequencePoint) = 
        let dcToInt (DocumentCoordinate dc) = dc
        { Id = sp.id.ToString().ToLowerInvariant()
          Document = sp.document.ToString()
          StartLine = sp.startLine |> dcToInt
          StartColumn = sp.startColumn |> dcToInt
          EndLine = sp.endLine |> dcToInt
          EndColumn = sp.endColumn |> dcToInt }

[<CLIMutable>]
type DsTestCase = 
    { TestCase : string
      [<Indexed>]
      CodeFilePath : string
      DisplayName : string
      ExtensionUri : string
      FullyQualifiedName : string
      [<PrimaryKey>]
      Id : Guid
      LineNumber : int
      Source : string }
    static member FromXTestCase(xtc : XTestCase) = 
        { DsTestCase.TestCase = xtc.TestCase
          CodeFilePath = xtc.CodeFilePath
          DisplayName = xtc.DisplayName
          ExtensionUri = xtc.ExtensionUri.ToString()
          FullyQualifiedName = xtc.FullyQualifiedName
          Id = xtc.Id
          LineNumber = xtc.LineNumber
          Source = xtc.Source }

[<CLIMutable>]
type DsTestResult = 
    { [<PrimaryKey>]
      Id : int
      DisplayName : string
      Outcome : string
      [<Indexed>]
      TestCaseId : Guid }
    static member FromXTestResult(xtr : XTestResult) = 
        { Id = xtr.GetHashCode()
          DisplayName = xtr.DisplayName
          Outcome = sprintf "%A" xtr.Outcome
          TestCaseId = xtr.TestCase.Id }

[<CLIMutable>]
type DsCoverageData = 
    { [<PrimaryKey>]
      [<AutoIncrement>]
      Id : int
      TestResultId : int
      SequencePointId : string }
    static member FromTestCoverageData trid = 
        Seq.map (fun (a, m, s) -> 
            { Id = 0
              TestResultId = trid
              SequencePointId = (sprintf "%s|%s|%s" a m s).ToLowerInvariant() })

[<CLIMutable>]
type DsFailureInfo = 
    { [<PrimaryKey>]
      [<AutoIncrement>]
      Id : int
      TestResultId : int
      FailureInfo : string }
    static member FromXFailureInfo trid (xfi : XTestFailureInfo) = 
        { Id = 0
          TestResultId = trid
          FailureInfo = xfi |> JsonContract.serialize }

let dbPath = 
    "tddstud10.datastore.db"
    |> Prelude.tuple2 (Path.getExecutingAssemblyLocation())
    |> Path.Combine

let initializeDb (db : SQLiteConnection option) = 
    db |> Option.iter (fun db -> db.Dispose())
    let db = 
        dbPath
        |> Prelude.tee File.Delete
        |> fun p -> new SQLiteConnection(p)
    db.CreateTable<DsSequencePoint>() |> ignore
    db.CreateTable<DsTestCase>() |> ignore
    db.CreateTable<DsTestResult>() |> ignore
    db.CreateTable<DsCoverageData>() |> ignore
    db.CreateTable<DsFailureInfo>() |> ignore
    db |> Some

let runInTransaction f (db : SQLiteConnection) = db.RunInTransaction(fun () -> f db)

let insertTestResults tr (db : SQLiteConnection) = 
    tr.Result
    |> DsTestResult.FromXTestResult
    |> db.Insert
    |> ignore
    let trhc = tr.GetHashCode()
    tr.CoverageData
    |> DsCoverageData.FromTestCoverageData trhc
    |> db.InsertAll
    |> ignore
    tr.Result.FailureInfo |> Option.iter (DsFailureInfo.FromXFailureInfo trhc
                                          >> db.Insert
                                          >> ignore)

let insertTestCase t (db : SQLiteConnection) = 
    t
    |> DsTestCase.FromXTestCase
    |> db.Insert
    |> ignore

let insertSequencePoints sps (db : SQLiteConnection) = 
    sps
    |> Seq.map DsSequencePoint.FromSequencePoint
    |> db.InsertAll
    |> ignore

let actorLoop (m : Actor<_>) = 
    m.Context.System.EventStream.Subscribe<DataStoreMessage>(m.Self) |> ignore
    let rec loop (db : SQLiteConnection option) = 
        actor { 
            let! msg = m.Receive()
            match msg with
            | DsInitialize(_) -> 
                let db = initializeDb db
                return! loop db
            | DsSequencePointsDiscovered(_, sps) -> 
                db |> Option.iter (sps.Values
                                   |> Seq.collect id
                                   |> insertSequencePoints
                                   |> runInTransaction)
                return! loop db
            | DsTestDiscovered(_, t) -> 
                db |> Option.iter (t
                                   |> insertTestCase
                                   |> runInTransaction)
                return! loop db
            | DsTestRunSucceeded(_, tr) -> 
                db |> Option.iter (tr
                                   |> insertTestResults
                                   |> runInTransaction)
                return! loop db
        }
    loop None
