module R4nd0mApps.TddStud10.Engine.Core.ContractTestHelpers

open Newtonsoft.Json
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Engine.Core
open System.Collections.Concurrent
open System.IO
open System.Reflection
open System.Text.RegularExpressions

let binRoot = Assembly.GetExecutingAssembly() |> Assembly.getAssemblyLocation

let getTestProjectsRoot testProject = 
    [ Path.GetFullPath(Path.Combine(binRoot, @"..\..\..\TestProjects"))
      Path.GetFullPath(Path.Combine(binRoot, @"..\TestProjects")) ]
    |> List.map (fun it -> Path.Combine(it, testProject))
    |> List.find File.Exists

let normalizeJsonDoc (binRoot : string) (root : string) = 
    let regexReplace (p : string, r : string) s = 
        Regex.Replace(s, p, r, RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
    [ @"[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?", "<GUID>"
      @"[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?\.[0-9]*\.[0-9]*", "<GUID>.X.Y"
      @"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]*Z", "<DATETIME>"
      
      @"\""ErrorMessage\""\: \""FsCheck\.Xunit\.PropertyFailedException \: .*Falsifiable, .*\""", 
      @"""ErrorMessage"": ""FsCheck.Xunit.PropertyFailedException : Falsifiable..."""
      binRoot.Replace(@"\", @"\\\\"), "<binroot>"
      root.Replace(@"\", @"\\\\"), "<root>" ]
    |> List.foldBack regexReplace

let collectEngineEvents (ees : IEngineEvents) = 
    let es = ConcurrentQueue<obj>()
    ees.RunStateChanged.Add(fun ea -> es.Enqueue([| ea |]))
    ees.RunStarting.Add(fun ea -> es.Enqueue([| ea |]))
    ees.RunStepStarting.Add(fun ea -> es.Enqueue([| ea |]))
    ees.RunStepEnded.Add(fun ea -> es.Enqueue([| ea.sp, ea.info |]))
    ees.RunStepError.Add(fun ea -> es.Enqueue([| ea.sp, ea.info |]))
    ees.RunEnded.Add(fun ea -> es.Enqueue([| ea |]))
    ees.RunError.Add(fun ea -> es.Enqueue([| ea.Message |]))
    es

let collectEngineEventsSummary (ees : IEngineEvents) = 
    let es = ConcurrentQueue<obj>()
    ees.RunStateChanged.Add(fun _ -> es.Enqueue("RunStateChanged"))
    ees.RunStarting.Add(fun _ -> es.Enqueue("RunStarting"))
    ees.RunStepStarting.Add(fun _ -> es.Enqueue("RunStepStarting"))
    ees.RunStepEnded.Add(fun _ -> es.Enqueue("RunStepEnded"))
    ees.RunStepError.Add(fun _ -> es.Enqueue("RunStepError"))
    ees.RunEnded.Add(fun _ -> es.Enqueue("RunEnded"))
    ees.RunError.Add(fun _ -> es.Enqueue("RunError"))
    es

let collectDataStoreEvents (dses : IXDataStoreEvents) = 
    let es = ConcurrentQueue<obj>()
    dses.CoverageInfoUpdated.Add(fun () -> es.Enqueue("CoverageInfoUpdated"))
    dses.SequencePointsUpdated.Add(fun () -> es.Enqueue("SequencePointsUpdated"))
    dses.TestCasesUpdated.Add(fun () -> es.Enqueue("TestCasesUpdated"))
    dses.TestFailureInfoUpdated.Add(fun () -> es.Enqueue("TestFailureInfoUpdated"))
    dses.TestResultsUpdated.Add(fun () -> es.Enqueue("TestResultsUpdated"))
    es

let rec waitWhileRunInProgress (e : IEngine) = 
    async { 
        do! Async.Sleep(1000)
        let! running = e.IsRunInProgress()
        if running then return! waitWhileRunInProgress e
        else return ()
    }
