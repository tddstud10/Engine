namespace R4nd0mApps.TddStud10.Engine.Core

module EngineConfigLoader = 
    open R4nd0mApps.TddStud10.Common
    open R4nd0mApps.TddStud10.Common.Domain
    open System.IO
    open Newtonsoft.Json

    let inline configPath (FilePath path) = path + ".tddstud10.user"
    
    let inline private toJson<'T> (cfg : 'T) = 
        JsonConvert.SerializeObject(cfg, Formatting.Indented, JsonSerializerSettings(DefaultValueHandling = DefaultValueHandling.Include));
    
    let inline private fromJson<'T> str : 'T = 
        JsonConvert.DeserializeObject<'T>(str, JsonSerializerSettings(DefaultValueHandling = DefaultValueHandling.Populate))
    
    let inline private fromJsonSafe<'T> str = 
        fun () -> JsonConvert.DeserializeObject<'T>(str, JsonSerializerSettings(DefaultValueHandling = DefaultValueHandling.Populate))
        |> Exec.safeExec2

    let defaultValue<'T> : 'T = "{}" |> fromJson<'T>

    let load<'T> slnPath : 'T = 
        let cfgPath = configPath slnPath
        
        let cfg, json = 
            if File.Exists(cfgPath) then 
                let cfg = 
                    File.ReadAllText(cfgPath)
                    |> fromJsonSafe
                    |> Option.fold (fun _ -> id) defaultValue<'T>
                cfg, toJson cfg
            else defaultValue<'T>, toJson defaultValue<'T>
        
        Exec.safeExec (fun () -> File.WriteAllText(cfgPath, json))
        cfg : 'T

    let setConfig<'T> slnPath (cfg : 'T) =
        let cfgPath = configPath slnPath
        File.WriteAllText(cfgPath, cfg |> toJson)
