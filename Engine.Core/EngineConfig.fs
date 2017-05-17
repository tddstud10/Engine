namespace R4nd0mApps.TddStud10.Engine.Core

open System.ComponentModel

type EngineConfig = 
    { [<DefaultValue(@"%temp%\_tdd")>]            
      SnapShotRoot : string

      [<DefaultValue("")>]            
      IgnoredTests : string

      [<DefaultValue(false)>]            
      IsDisabled : bool

      [<DefaultValue([|"_TDDSTUD10"|])>]            
      AdditionalMSBuildProperties : string[]

      [<DefaultValue([|"packages"; "paket-files"|])>]  
      SnapshotIncludeFolders : string[]

      [<DefaultValue([|".git"; "obj"; "bin"|])>] 
      SnapshotExcludeFolders : string[] }
