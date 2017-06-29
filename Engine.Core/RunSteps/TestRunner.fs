module R4nd0mApps.TddStud10.Engine.RunSteps.TestRunner

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open R4nd0mApps.TddStud10.TestHost
open R4nd0mApps.XTestPlatform.Api

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

let runTest (svc : ITestAdapterService) rsp (t : XTestCase) f = 
    async { 
        let teSearchPath = 
            [ rsp.SolutionPaths.Path |> FilePath.getDirectoryName
              FilePath "packages" ]
            |> FilePath.combine
        
        let tr = 
            t
            |> DataContract.serialize
            |> svc.ExecuteTestsAndCollectCoverageData teSearchPath
        
        (rsp, tr) |> f
        return tr
    }
