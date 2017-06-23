module R4nd0mApps.TddStud10.Engine.Core.EngineConfigLoaderTests

open R4nd0mApps.TddStud10.Common.Domain
open System
open System.IO
open global.Xunit
open FsUnit.Xunit
open System.ComponentModel

type AConfig =
    { [<DefaultValue("aSetting default value")>] 
      ASetting : string

      [<DefaultValue(0xdeadbeeful)>]
      BSetting : uint32 }

let getSlnPath sln = sprintf @"%s.%s" (Path.GetTempFileName()) sln |> FilePath

let defCfg = { ASetting = "aSetting default value"; BSetting = 0xdeadbeeful }
let defCfgStr = 
    "{\n  \"ASetting\": \"aSetting default value\",\n  \"BSetting\": 3735928559\n}"
    |> String.replace "\n" Environment.NewLine

[<Fact>]
let ``First time load creates file and returns default value``() = 
    let sln = getSlnPath "first.sln"
    let cfg = EngineConfigLoader.load<AConfig> sln
    
    cfg |> should equal defCfg
    File.ReadAllText(EngineConfigLoader.configPath sln) |> should equal defCfgStr

    File.Delete(EngineConfigLoader.configPath sln)

[<Fact>]
let ``First time load creates file and returns default value, even if save fails``() = 
    let sln = getSlnPath "|/first.sln"
    let cfg = EngineConfigLoader.load<AConfig> sln
    cfg |> should equal defCfg
    File.Exists(EngineConfigLoader.configPath sln) |> should equal false

[<Fact>]
let ``Second time load reads from file and returns value``() = 
    let sln = getSlnPath "second.sln"
    let cfg0 = EngineConfigLoader.load<AConfig> sln

    File.WriteAllText(EngineConfigLoader.configPath sln, defCfgStr.Replace(": 3735928559", ": 100"))
    let cfg = EngineConfigLoader.load sln
    cfg.ASetting |> should equal cfg0.ASetting
    cfg.BSetting |> should equal 100ul

    File.Delete(EngineConfigLoader.configPath sln)

[<Fact>]
let ``Second time load with corrupted file, returns default values and recreates file``() = 
    let sln = getSlnPath "first.sln"
    EngineConfigLoader.load<AConfig> sln |> ignore
    File.WriteAllText(EngineConfigLoader.configPath sln, "{\"aSetting :\"aSetting default value\"}")

    let cfg = EngineConfigLoader.load<AConfig> sln
    cfg |> should equal defCfg
    File.ReadAllText(EngineConfigLoader.configPath sln) |> should equal defCfgStr

    File.Delete(EngineConfigLoader.configPath sln)

[<Fact>]
let ``Second time load with some values, override those and return default value for others``() = 
    let sln = getSlnPath "second.sln"
    File.WriteAllText(EngineConfigLoader.configPath sln, "{\"ASetting\":\"changed aSetting value\"}")
    let cfg = EngineConfigLoader.load<AConfig> sln

    cfg.ASetting |> should equal "changed aSetting value"
    Assert.Equal(cfg.BSetting, defCfg.BSetting)

    File.Delete(EngineConfigLoader.configPath sln)
