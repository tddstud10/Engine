﻿namespace R4nd0mApps.TddStud10.Engine.Core

[<AutoOpen>]
module RunStartParamsExtensions = 
    open R4nd0mApps.TddStud10.Common
    open System
    open System.IO
    open System.Reflection
    open R4nd0mApps.TddStud10.Common.Domain
    open R4nd0mApps.TddStud10
    
    let testHostProcessName = sprintf "TddStud10.TestHost%s.exe" (if (Common.DFizer.isDF()) then ".DF" else "")
    
    type RunStartParams with
        static member Create (cfg : EngineConfig) startTime solutionPath = 
            let snapShotRoot = Environment.ExpandEnvironmentVariables(cfg.SnapShotRoot) |> FilePath
            let buildRoot = PathBuilder.makeSlnBuildRoot snapShotRoot solutionPath
            { StartTime = startTime
              TestHostPath = Path.Combine(Path.getExecutingAssemblyLocation(), testHostProcessName) |> FilePath
              Solution = 
                  { Path = solutionPath
                    SnapshotPath = PathBuilder.makeSlnSnapshotPath snapShotRoot solutionPath
                    BuildRoot = buildRoot }
              Config = { cfg with SnapShotRoot = snapShotRoot.ToString() }
              DataFiles = 
                  { SequencePointStore =
                        FilePath.combine [ buildRoot
                                           FilePath "Z_sequencePointStore.xml" ]
                    CoverageSessionStore = 
                        FilePath.combine [ buildRoot
                                           FilePath "Z_coverageresults.xml" ]
                    TestResultsStore = 
                        FilePath.combine [ buildRoot
                                           FilePath "Z_testresults.xml" ]
                    DiscoveredUnitTestsStore = 
                        FilePath.combine [ buildRoot
                                           FilePath "Z_discoveredUnitTests.xml" ]
                    DiscoveredUnitDTestsStore = 
                        FilePath.combine [ buildRoot
                                           FilePath "Z_discoveredUnitDTests.xml" ]
                    TestFailureInfoStore = 
                        FilePath.combine [ buildRoot
                                           FilePath "Z_testFailureInfo.xml" ] } }
