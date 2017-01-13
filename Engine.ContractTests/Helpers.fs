namespace R4nd0mApps.TddStud10.Engine.Core

module Helpers = 
    open Newtonsoft.Json
    open R4nd0mApps.TddStud10.Common
    open R4nd0mApps.TddStud10.Common.Domain
    open R4nd0mApps.TddStud10.Engine.Core
    open System
    open System.IO
    open System.Reflection
    open System.Text.RegularExpressions
    open System.Collections.Concurrent

    let binRoot = 
        Assembly.GetExecutingAssembly() |> Assembly.getAssemblyLocation
            
    let getTestProjectsRoot testProject = 
        [ Path.GetFullPath
              (Path.Combine(binRoot, @"..\..\..\TestProjects"))
          Path.GetFullPath(Path.Combine(binRoot, @"..\TestProjects")) ]
        |> List.map (fun it -> Path.Combine(it, testProject))
        |> List.find File.Exists
    
    let cfg = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    let toJson o = JsonConvert.SerializeObject(o, Formatting.Indented, cfg)
    
    let normalizeJsonDoc (binRoot : string) (root : string) = 
        let regexReplace (p : string, r : string) s = Regex.Replace(s, p, r, RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
        [ @"[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?", "<GUID>"
          @"[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?\.[0-9]*\.[0-9]*", "<GUID>.X.Y"
          @"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]*Z", "<DATETIME>"
          @"\""ErrorMessage\""\: \""FsCheck\.Xunit\.PropertyFailedException \: .*Falsifiable, .*\""", @"""ErrorMessage"": ""FsCheck.Xunit.PropertyFailedException : Falsifiable..."""
          binRoot.Replace(@"\", @"\\\\"), "<binroot>"
          root.Replace(@"\", @"\\\\"), "<root>" ]
        |> List.foldBack regexReplace

    let collectEngineEvents (ees : IEngineEvents) =
        let es = ConcurrentQueue<obj>()
        ees.RunStateChanged.Add(fun ea -> es.Enqueue([|ea|]))
        ees.RunStarting.Add(fun ea -> es.Enqueue([|ea|]))
        ees.RunStepStarting.Add(fun ea -> es.Enqueue([|ea|]))
        ees.RunStepEnded.Add(fun ea -> es.Enqueue([|ea.sp, ea.info|]))
        ees.RunStepError.Add(fun ea -> es.Enqueue([|ea.sp, ea.info|]))
        ees.RunEnded.Add(fun ea -> es.Enqueue([|ea|]))
        ees.RunError.Add(fun ea -> es.Enqueue([|ea.Message|]))
        es
    
    let collectDataStoreEvents (dses : IXDataStoreEvents) =
        let es = ConcurrentQueue<obj>()
        dses.CoverageInfoUpdated.Add (fun () -> es.Enqueue("CoverageInfoUpdated")) 
        dses.SequencePointsUpdated.Add (fun () -> es.Enqueue("SequencePointsUpdated")) 
        dses.TestCasesUpdated.Add (fun () -> es.Enqueue("TestCasesUpdated")) 
        dses.TestFailureInfoUpdated.Add (fun () -> es.Enqueue("TestFailureInfoUpdated")) 
        dses.TestResultsUpdated.Add (fun () -> es.Enqueue("TestResultsUpdated")) 
        es

    let normalizeRunOutput eEvents dsEvents =
        [ eEvents :> obj
          dsEvents :> obj ]

    let runEngine sln props = 
        let ssr = sprintf @"%s\%O" binRoot (Guid.NewGuid())
        let h = TddStud10HostProxy(9999, @"engine\contracttests", true) :> ITddStud10Host
        try 
            let ds = h.GetDataStore()
            let eEvents = collectEngineEvents <| h.GetEngineEvents()
            let dsEvents = collectDataStoreEvents <| h.GetDataStoreEvents()

            let ver = h.Start() |> Async.RunSynchronously
            Xunit.Assert.Equal(ver, App.getVersion())

            let e = h.GetEngine()
            Xunit.Assert.False(e.IsEnabled() |> Async.RunSynchronously)

            let x = e.EnableEngine() |> Async.RunSynchronously
            Xunit.Assert.True(x)
            Xunit.Assert.True(e.IsEnabled() |> Async.RunSynchronously)

            let x = e.DisableEngine() |> Async.RunSynchronously
            Xunit.Assert.False(x)
            Xunit.Assert.False(e.IsEnabled() |> Async.RunSynchronously)

            e.EnableEngine() |> Async.RunSynchronously |> ignore

            let ep = 
                { HostVersion = HostVersion.VS2015; 
                    EngineConfig = EngineConfig(SnapShotRoot = ssr, AdditionalMSBuildProperties = props); 
                    SolutionPath = getTestProjectsRoot sln |> FilePath; 
                    SessionStartTime = DateTime.UtcNow.AddMinutes(-1.0) }
            let runInProgress = 
                e.RunEngine ep
                |> Async.RunSynchronously
            Xunit.Assert.True(runInProgress)

            while e.IsRunInProgress() |> Async.RunSynchronously do
                System.Threading.Thread.Sleep(1000)

            let runOutput = sprintf "[%s\r\n,%s]" (normalizeRunOutput (eEvents.ToArray()) (dsEvents.ToArray()) |> toJson) (ds.GetSerializedState() |> Async.RunSynchronously)

            h.Stop()
            Xunit.Assert.False(h.IsRunning)
            
            runOutput, ep.SolutionPath
        finally
            h.Stop()
            if Directory.Exists ssr then Directory.Delete(ssr, true)
