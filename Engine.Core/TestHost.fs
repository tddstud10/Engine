namespace R4nd0mApps.TddStud10.Engine.Core

module TestHost =
    open System

    let buildCommandLine cmd (rsp : RunStartParams) =
        sprintf "\"%s\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\" \"%O\""
            cmd
            rsp.Solution.BuildRoot
            rsp.DataFiles.CoverageSessionStore
            rsp.DataFiles.TestResultsStore
            rsp.DataFiles.DiscoveredUnitTestsStore
            rsp.DataFiles.TestFailureInfoStore
            rsp.StartTime.Ticks
            rsp.Solution.Path
            rsp.Solution.SnapshotPath
            rsp.DataFiles.DiscoveredUnitDTestsStore
            (String.Join(",", rsp.Config.IgnoredTests))
