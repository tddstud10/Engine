module R4nd0mApps.TddStud10.Engine.RunSteps.AssemblySequencePointsDiscoverer

open R4nd0mApps.TddStud10.Engine.Core

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

let discoverAssemblySequencePoints f rsp (pbo : ProjectBuilderOutput) = 
    async { 
        let! _ = pbo.Items
                 |> Array.map 
                        (fun fp -> 
                        EngineAPI.invokeInstrumentationAPITempHack<PerDocumentSequencePoints2> "GenerateSequencePointInfo2" 
                            [| fp; pbo.Project.Path; pbo.SnapshotPath |] |> Async.map (Prelude.tuple2 rsp >> f))
                 |> Async.Parallel
        return pbo
    }
