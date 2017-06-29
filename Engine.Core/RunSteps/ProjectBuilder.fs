module R4nd0mApps.TddStud10.Engine.RunSteps.ProjectBuilder

open Microsoft.Build.Execution
open Microsoft.Build.Framework
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

type BuildLogger() = 
    inherit Microsoft.Build.Utilities.Logger()
    let warnings = ConcurrentQueue<string>()
    let errors = ConcurrentQueue<string>()
    member __.Warnings = warnings
    member __.Errors = errors
    override __.Initialize(es : IEventSource) = 
        es.WarningRaised.Add
            (fun w -> 
            warnings.Enqueue
                (sprintf "%s(%d,%d): %s error %s: %s" w.File w.LineNumber w.ColumnNumber w.Subcategory w.Code w.Message))
        es.ErrorRaised.Add
            (fun e -> 
            errors.Enqueue
                (sprintf "%s(%d,%d): %s error %s: %s" e.File e.LineNumber e.ColumnNumber e.Subcategory e.Code e.Message))

let private wrapperProjectName = "_tddstud10wrapper.proj"
let private wrapperProjectContents = """<Project ToolsVersion="14.0" DefaultTargets="_TddStud10BuildProject" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="_TddStud10BuildProject">
        <MSBuild 
            Projects="$(_TddStud10Project)" 
            Targets="$(_TddStud10Target)"
        >
            <Output TaskParameter="TargetOutputs" ItemName="_TddStud10TargetOutputs" />
        </MSBuild>
    </Target>
</Project>"""

let buildProject rsp (sso : ProjectSnapshotCreatorOutput) = 
    let isAssembly = 
        FilePath.getExtension
        >> String.toLowerInvariant
        >> Prelude.flip List.contains [ ".dll"; ".exe" ]
    async { 
        let wrapperProjectPath = 
            [ sso.SnapshotDirectoryName
              wrapperProjectName |> FilePath ]
            |> FilePath.combine
        FilePath.writeAllText wrapperProjectContents wrapperProjectPath
        let props = 
            rsp.Config.AdditionalMSBuildProperties
            |> Array.map (String.split '=' >> function 
                          | [ p; v ] -> Some(p, v)
                          | _ -> None)
            |> Array.choose id
            |> Array.fold (fun acc (p, v) -> Map.add p v acc) Map.empty
        
        let props = 
            [ "_TddStud10Project", sso.SnapshotPath.ToString()
              "_TddStud10Target", "Build"
              "Configuration", "Debug"
              "CreateVsixContainer", "false"
              "DeployExtension", "false"
              "CopyVsixExtensionFiles", "false"
              "RunCodeAnalysis", "false"
              "DeployOnBuild", "false"
              "DebugSymbols", "true"
              "DebugType", "full"
              "Optimize", "false"
              "_TDDSTUD10", "1"
              "VisualStudioVersion", "14.0" ]
            |> List.fold (fun acc (p, v) -> Map.add p v acc) props
        
        let props = props :> IDictionary<_, _>
        let l = BuildLogger()
        let proj = ProjectInstance(wrapperProjectPath.ToString(), props, "14.0")
        let status = proj.Build([| "_TddStud10BuildProject" |], [ l :> ILogger ])
        
        let outputs = 
            proj.GetItems("_TddStud10TargetOutputs")
            |> Seq.map (fun i -> i.EvaluatedInclude |> FilePath)
            |> Seq.filter isAssembly
        if not status then 
            l.Warnings
            |> Seq.append l.Errors
            |> Seq.fold (fun (acc : StringBuilder) -> acc.AppendLine) (StringBuilder())
            |> failwithf "Build Errors %O"
        return { Items = outputs |> Array.ofSeq
                 SnapshotDirectoryName = sso.SnapshotDirectoryName
                 Project = sso.Project }
    }
