module R4nd0mApps.TddStud10.Engine.Actors.ADataStore

open ActorMessages
open Akka.Event
open Akka.FSharp
open SQLite
open System.IO

[<CLIMutable>]
type Table1 = 
    { [<PrimaryKey>]
      Field1 : int
      Field2 : string }

let actorLoop (m : Actor<_>) = 
    m.Context.System.EventStream.Subscribe<DataStoreMessage>(m.Self) |> ignore
    let rec loop (db : SQLiteConnection option) = 
        actor { 
            let! msg = m.Receive()
            match msg with
            | DsInitialize -> 
                db |> Option.iter (fun db -> db.Dispose())
                File.Delete(Path.Combine(Path.getLocalPath(), "tddstud10.datastore.db"))
                let db = new SQLiteConnection(Path.Combine(Path.getLocalPath(), "tddstud10.datastore.db"))
                db.CreateTable<Table1>() |> ignore
                return! loop (Some db)
        }
    loop None
