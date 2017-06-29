module R4nd0mApps.TddStud10.Engine.RunSteps.AssemblyTestsDiscoverer

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.TestHost

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

let discoverAssemblyTests (svc : ITestAdapterService) rsp pbo = 
    async { 
        let rebasePaths = pbo.Project.Path, pbo.SnapshotPath
        
        let tdSearchPath = 
            [ rsp.SolutionPaths.Path |> FilePath.getDirectoryName
              FilePath "packages" ]
            |> FilePath.combine
        pbo.Items |> Array.iter (svc.DiscoverTests rebasePaths tdSearchPath rsp.Config.IgnoredTests)
        return pbo
    }
