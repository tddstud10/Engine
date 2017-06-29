module R4nd0mApps.TddStud10.Engine.RunSteps.AssemblyInstrumenter

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.TestRuntime

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

let instrumentAssembly _ pbo = 
    async { 
        let! _ = pbo.Items
                 |> Array.map 
                        (fun fp -> 
                        EngineAPI.invokeInstrumentationAPITempHack<unit> "Instrument2" 
                            [| fp; pbo.Project.Path; pbo.SnapshotPath |])
                 |> Async.Parallel
        pbo.Items
        |> Seq.map FilePath.getDirectoryName
        |> Seq.iter (fun d -> 
               let s = TestRunTimeInstaller.Install(d.ToString())
               logger.logInfof "Installing test runtine: %s" s)
        return pbo
    }
