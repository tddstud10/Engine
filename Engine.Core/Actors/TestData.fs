module R4nd0mApps.TddStud10.Engine.Actors.TestData

open System
open System.IO
open System.Diagnostics
open R4nd0mApps.TddStud10.Common
open Hekate
open System.Collections.Generic
open System.Collections.ObjectModel

[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type ProjectId = 
    { UniqueName : string
      Id : Guid }
    member self.AsString = sprintf "%s (%O)" self.UniqueName self.Id

[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type Project = 
    { Id : ProjectId
      Path : Domain.FilePath
      Items : Domain.FilePath[] }
    member self.AsString = 
        sprintf "%O (Items = %d)" self.Path (self.Items |> Seq.length)

type ProjectMap = Map<ProjectId, Project>

[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type Solution = 
    { Path : Domain.FilePath
      Projects : Project[]
      DependencyMap : IReadOnlyDictionary<ProjectId, seq<ProjectId>> }
    member it.AsString = sprintf "%O (Dependencies: %d)" it.Path it.DependencyMap.Count
    member it.DGraph =
        Graph.create 
            (it.Projects |> Array.map (fun p -> p.Id, p.Id.ToString()) |> Array.toList)
            (it.DependencyMap |> Seq.map (fun kvp -> kvp.Value |> Seq.map (fun p2 -> kvp.Key, p2, sprintf "%O -> %O" kvp.Key p2)) |> Seq.collect id |> Seq.toList)
            
[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type AssemblyId = 
    { Path : Domain.FilePath
      Project : ProjectId }
    member it.AsString = sprintf "Assembly: %O [%A]" it.Path it.Project
            
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

let inline (~~) x = Domain.FilePath x

let Projects = 
        [| 
          { Id = { UniqueName = @"core\core.csproj"
                   Id = Guid("b749b8e1-3779-4d2a-86c0-a727ff368d2d") }
            Path = ~~ @"c:\projects\testsln\core\core.csproj"
            Items = [| ~~ @"c:\projects\testsln\core\core.cs" |] } 

          { Id = { UniqueName = @"lib1\lib1.csproj"
                   Id = Guid("b749b8e2-3779-4d2a-86c0-a727ff368d2d") }
            Path = ~~ @"c:\projects\testsln\lib1\lib1.csproj"
            Items = [| ~~ @"c:\projects\testsln\lib1\lib1.cs" |] } 

          { Id = { UniqueName = @"lib1.tests\lib1.tests.csproj"
                   Id = Guid("b749b8e3-3779-4d2a-86c0-a727ff368d2d") }
            Path = ~~ @"c:\projects\testsln\lib1.tests\lib1.tests.csproj"
            Items = [| ~~ @"c:\projects\testsln\lib1.tests\lib1.tests.cs" |] } 

          { Id = { UniqueName = @"lib2\lib2.csproj"
                   Id = Guid("b749b8e4-3779-4d2a-86c0-a727ff368d2d") }
            Path = ~~ @"c:\projects\testsln\lib2\lib2.csproj"
            Items = [| ~~ @"c:\projects\testsln\lib2\lib2.cs" |] } 
        |]

let Assemblies =
    Projects
    |> Array.map (fun p -> p.Id, ~~ Path.Combine(Path.GetDirectoryName(p.Path.ToString()), @"bin\Debug", Path.GetFileNameWithoutExtension(p.Path.ToString()) + ".dll"))
    |> Map.ofArray

let Tests =
    [ Assemblies |> Map.find Projects.[2].Id, ["Test 1"; "Test 2"; "Test 3"; "Test 4"; "Test 5"] ] |> Map.ofList

let Solution = 
    { Path = ~~ @"c:\projects\testsln\test.sln"
      Projects = Projects
      DependencyMap = 
        [
            (Projects.[0].Id, [] |> Seq.ofList)
            (Projects.[1].Id, [ Projects.[0].Id ] |> Seq.ofList)
            (Projects.[2].Id, [ Projects.[1].Id ] |> Seq.ofList)
            (Projects.[3].Id, [ Projects.[0].Id ] |> Seq.ofList)
        ] |> dict |> ReadOnlyDictionary<_, _> }

module API =
    let instrumentAssembly rid a =
        async {
            do! Async.Sleep(1000)
        }
    
    let buildProject rid p =
        async {
            do! Async.Sleep(1000)
            return { Path = Assemblies |> Map.find p; Project = p }
        }

    let fixProjectFile rid p =
        async {
            do! Async.Sleep(1000)
        }

    let createProjectSnapshot rid (p : ProjectId) =
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

