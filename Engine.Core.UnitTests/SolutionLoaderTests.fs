module R4nd0mApps.TddStud10.Engine.Core.SolutionLoaderTests

open FsUnit.Xunit
open R4nd0mApps.TddStud10.Common.Domain
open global.Xunit
open System.IO

let binRoot = Path.getLocalPath()

let getTestProjectsRoot testProject = 
    [ Path.GetFullPath(Path.Combine(binRoot, @"..\..\..\TestProjects"))
      Path.GetFullPath(Path.Combine(binRoot, @"..\TestProjects")) ]
    |> List.map (fun it -> Path.Combine(it, testProject))
    |> List.find File.Exists

[<Fact>]
let ``loadSolution should build Solution from filesystem``() = 
    let slnPath = @"ComplexSln\ComplexSln.sln" |> getTestProjectsRoot |> FilePath
    let sln = SolutionLoader.load [| @"\.git\"; @"\bin\"; @"\xbin\"; @"\obj\" |] slnPath
    sln.Path |> should equal slnPath
    sln.Projects
    |> Array.map (fun p -> p.Name)
    |> Array.sort
    |> should equal [| "Lib1"; "Lib2"; "Lib3" |]
    sln.Projects
    |> Array.map (fun p -> p.Items)
    |> Array.collect id
    |> Array.length
    |> should equal 16
    let x =
        sln.Projects
        |> Array.filter (fun p -> p.Name = "Lib1")
        |> Array.collect (fun p -> p.Items)
        |> Array.sort
    let y = [| FilePath "Class1.cs"; FilePath "Lib1.csproj"; FilePath @"Properties\AssemblyInfo.cs" |]
    x |> should equal y
    sln.DependencyMap
    |> Seq.map (fun kv -> (kv.Key.Name, kv.Value |> Seq.length))
    |> Seq.sortBy fst
    |> Seq.toList
    |> should equal [ ("Lib1", 0)
                      ("Lib2", 1)
                      ("Lib3", 2) ]

// TODO: Move this to Common.UnitTests
[<Theory>]
[<InlineData(@"c:\a\", @"c:\a\b 1.txt", @"b 1.txt")>]
[<InlineData(@"c:\a\c", @"c:\a\c\b.txt", @"b.txt")>]
[<InlineData(@"c:\a\", @"c:\a\b.txt", @"b.txt")>]
[<InlineData(@"c:\a", @"c:\a\c\b.txt", @"c\b.txt")>]
[<InlineData(@"c:\a\b\", @"c:\a\c\b.txt", @"..\c\b.txt")>]
let ``makeRelativePath tests`` folder abs rel =
    FilePath.makeRelativePath (FilePath folder) (FilePath abs) |> should equal (FilePath rel)