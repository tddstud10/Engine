module R4nd0mApps.TddStud10.Engine.Actors.TestData

open System
open System.IO
open System.Diagnostics
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
            
[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type AssemblyId = 
    { Path : FilePath
      Project : ProjectId }
    member it.AsString = sprintf "Assembly: %O [%O]" it.Path it.Project
            
[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type TestId = 
    { TestName : string
      Assembly : AssemblyId }
    member it.AsString = sprintf "Test: %O [%A]" it.TestName it.Assembly

type FailureInfo =
    { Message : string
      StackTrace : string }
    static member FromException (e : exn) =
        { Message = e.Message 
          StackTrace = e.StackTrace }

type RunId = Guid

// ---------------------------------------------------------------------------------------------------------

let inline (~~) x = FilePath x

let Tests =
    [ ] |> Map.ofList

module API =
    let createProjectSnapshot rid (p : ProjectId) =
        async {
            do! Async.Sleep(1000)
        }

    let fixProjectFile rid p =
        async {
            do! Async.Sleep(1000)
        }
    
    let buildProject rid (p : ProjectId) =
        async {
            do! Async.Sleep(1000)
            return { Path = Path.ChangeExtension(p.Path.ToString(), "dll") |> FilePath; Project = p }
        }

    let instrumentAssembly rid a =
        async {
            do! Async.Sleep(1000)
        }

    let discoverAssemblySequencePoints f rid a =
        async {
            do! Async.Sleep(1000)
            Tests 
            |> Map.tryFind a.Path
            |> Option.fold (fun _ x -> x) []
            |> List.map (fun t -> rid, { TestName = t; Assembly = a })
            |> List.iter f
        }

    let discoverAssemblyTests f rid a =
        async {
            do! Async.Sleep(1000)
            Tests 
            |> Map.tryFind a.Path
            |> Option.fold (fun _ x -> x) []
            |> List.map (fun t -> rid, { TestName = t; Assembly = a })
            |> List.iter f
        }

    let runTest rid t =
        async {
            if t.TestName = "Test 1" then
                failwithf "Test failed"

            do! Async.Sleep(1000)
        }

