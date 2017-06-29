module R4nd0mApps.TddStud10.Engine.Serverv2

open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.Logger
open System
open Akka.Actor
open Akka.FSharp
open R4nd0mApps.TddStud10.Engine.Actors
open R4nd0mApps.TddStud10.Engine.Actors.ActorMessages
open R4nd0mApps.TddStud10.Common.Domain

let logger = LoggerFactory.logger



(* 
   TODO: PARTHO: 
   - Split out the Engine and DataStore interactions into their individual classes/sockets/agents/etc.
   - Break out code units into their individual files
*)

(*

Directions
- Basics 
  - datastore
  - events
  - incremental
- VS
- VSCode on Win
- VS for macOS
  - adapter


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
- support nunit 
- Console log to actual logs
- Redirect akka.net Logging to our logger

- Snapshot
  - IncludeFolder settings
- Multi-project solutions
- Instrumentation should support snk
- run tests serially doesnt work

-------

- Use cancel of ITestExecutor
- IDE integration
  - IDE Events 
    - Specifics of each step - names of projects discovered, etc.
- Incremental
  - Test discover method will be different, will do lookup on files changed
  - Combined data since last resync
x Seperate executable for build and test
x Cooperative cancellation + assign cooperative cancellation
x Simplify messaging - ProjectBuildDone and BuildDone should ideally be the same

*)


let main _ = 
    let system = System.create ActorNames.System.Name (Configuration.load())

    spawn system ActorNames.IdEvents.Name IdeEvents.actorLoop |> ignore
    spawn system ActorNames.DataStore.Name ADataStore.actorLoop |> ignore

    let runner = spawn system ActorNames.Runner.Name Runner.actorLoop

    let rsp =
        { Id = Guid.NewGuid()
          SolutionPaths =
            if Environment.IsMono then
                { Path = @"/Users/partho/src/gh/t/Engine/TestProjects/FSXUnit2xNUnit2x.NET45/FSXUnit2xNUnit2x.sln" |> FilePath
                  SnapshotPath = @"/Users/partho/src/delme/_tdd/FSXUnit2xNUnit2x.NET45/FSXUnit2xNUnit2x.sln" |> FilePath
                  BuildRoot = "" |> FilePath }
            else
                { Path = @"D:\src\t\Engine\TestProjects\FSXUnit2xNUnit2x.NET45\FSXUnit2xNUnit2x.sln" |> FilePath
                  SnapshotPath = @"D:\delme\_tdd\FSXUnit2xNUnit2x.NET45\FSXUnit2xNUnit2x.sln" |> FilePath
//            { Path = @"d:\src\t\Engine\TestProjects\CSXUnit1xNUnit3x.NET20\CSXUnit1xNUnit3x.sln" |> FilePath
//              SnapshotPath = @"D:\delme\_tdd\CSXUnit1xNUnit3x.NET20\CSXUnit1xNUnit3x.sln" |> FilePath
                  BuildRoot = "" |> FilePath }
          Config = { EngineConfigLoader.defaultValue<_> with AdditionalMSBuildProperties = [|"DefineConstants=BREAK_TEST"|] } }

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

