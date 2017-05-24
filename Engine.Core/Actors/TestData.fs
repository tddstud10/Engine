module R4nd0mApps.TddStud10.Engine.Actors.TestData

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System
open System.Diagnostics
open System.IO

type FailureInfo = 
    { Message : string
      StackTrace : string }
    static member FromException(e : exn) = 
        { Message = e.Message
          StackTrace = e.StackTrace }

type RunId = Guid

// ---------------------------------------------------------------------------------------------------------
let inline (~~) x = FilePath x

module API = 
    open Microsoft.Build.Execution
    open Microsoft.Build.Framework
    open R4nd0mApps.TddStud10.Common
    open System.Collections.Concurrent
    open System.Collections.Generic
    open System.Reflection
    open System.Text
    open R4nd0mApps.TddStud10.TestHost
    
    let createProjectSnapshot rsp p = 
        let updateProjectItemSnapshot snapshotPath pi = 
            async { 
                let srcInfo = 
                    (p.DirectoryName.ToString(), pi.ToString())
                    |> Path.Combine
                    |> FileInfo
                
                let dstInfo = 
                    (snapshotPath.ToString(), pi.ToString())
                    |> Path.Combine
                    |> FileInfo
                
                let dstFolder = dstInfo.Directory.FullName
                if srcInfo.LastWriteTimeUtc > dstInfo.LastWriteTimeUtc then 
                    dstFolder
                    |> Directory.CreateDirectory
                    |> ignore
                    File.Copy(srcInfo.FullName, dstInfo.FullName, true)
            }
        async { 
            let snapshotDir = 
                rsp.SolutionPaths.SnapshotPath
                |> FilePath.getDirectoryName
                |> Prelude.flip FilePath.combine (p.Index.ToString() |> FilePath)
            let! _ = p.Items
                     |> Array.map (updateProjectItemSnapshot snapshotDir)
                     |> Async.Parallel
            return { ProjectSnapshotCreatorOutput.SnapshotDirectoryName = snapshotDir
                     Project = p }
        }
    
    (*
    let fixupProject bos (psnPath : FilePath) = 
        let createFileRefIGFragment inc (hp : FilePath) = 
            XElement
                (XName.Get("ItemGroup", "http://schemas.microsoft.com/developer/msbuild/2003"), 
                 XElement
                     (XName.Get("Reference", "http://schemas.microsoft.com/developer/msbuild/2003"), 
                      XAttribute(XName.Get("Include"), inc), 
                      XElement(XName.Get("HintPath", "http://schemas.microsoft.com/developer/msbuild/2003"), hp.ToString()), 
                      XElement(XName.Get("Private", "http://schemas.microsoft.com/developer/msbuild/2003"), "True")))
        let xdoc = XDocument.Load(psnPath.ToString())
        let xnm = XmlNamespaceManager(NameTable())
        xnm.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
        let ig1 = Extensions.XPathSelectElements(xdoc, "//msb:ItemGroup", xnm) |> Seq.nth 0
        bos
        |> Seq.iter (fun o -> 
               let oName = o |> Path.getFileNameWithoutExtension
               (oName, o)
               ||> createFileRefIGFragment
               |> ig1.AddBeforeSelf)
        Extensions.XPathSelectElements(xdoc, "//msb:ProjectReference", xnm) |> Seq.iter (fun x -> x.Remove())
        xdoc.Save(psnPath.ToString())
        psnPath
    
*)
    let fixProjectFile rsp p = async { return p }
    
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
                    (sprintf "%s(%d,%d): %s error %s: %s" w.File w.LineNumber w.ColumnNumber w.Subcategory w.Code 
                         w.Message))
            es.ErrorRaised.Add
                (fun e -> 
                errors.Enqueue
                    (sprintf "%s(%d,%d): %s error %s: %s" e.File e.LineNumber e.ColumnNumber e.Subcategory e.Code 
                         e.Message))
    
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
            let wrapperProjectPath = (sso.SnapshotDirectoryName, wrapperProjectName |> FilePath) ||> FilePath.combine
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
                  "_TDDSTUD10", "1" ]
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
    
    let invokeInstrumentationAPI<'T> methName pp pssp ap = 
        async { 
            let ret = 
                sprintf "R4nd0mApps.TddStud10.Engine%s" (if DFizer.isDF() then ".DF"
                                                         else "")
                |> Assembly.Load
                |> fun a -> a.GetType("R4nd0mApps.TddStud10.Instrumentation")
                |> fun t -> t.GetMethod(methName, BindingFlags.Public ||| BindingFlags.Static)
                |> fun m -> m.Invoke(null, [| ap; pp; pssp |]) 
            return ret :?> 'T
        }
    
    let instrumentAssembly _ pbo = 
        async { 
            let! _ = pbo.Items
                     |> Array.map (invokeInstrumentationAPI<unit> "Instrument2" pbo.Project.Path pbo.SnapshotPath)
                     |> Async.Parallel
            return pbo
        }
    
    let discoverAssemblySequencePoints f rsp (pbo : ProjectBuilderOutput) = 
        async { 
            let! _ = pbo.Items
                     |> Array.map 
                            (invokeInstrumentationAPI<PerDocumentSequencePoints2> "GenerateSequencePointInfo2" 
                                 pbo.Project.Path pbo.SnapshotPath >> Async.map (Prelude.tuple2 rsp >> f))
                     |> Async.Parallel
            return pbo
        }

    let discoverAssemblyTests f rsp pbo = 
        async {
            let rebasePaths = pbo.Project.Path, pbo.SnapshotPath
            let tdSearchPath =
                rsp.SolutionPaths.Path
                |> FilePath.getDirectoryName
                |> Prelude.flip FilePath.combine (FilePath "packages")

            let! _ = 
                pbo.Items
                |> Array.map (fun item -> async { return TestAdapterExtensions.discoverTests rebasePaths tdSearchPath rsp.Config.IgnoredTests item })
                |> Async.Parallel
                |> Async.map (Seq.collect id >> Seq.iter (Prelude.tuple2 rsp >> f))
    
            return pbo
        }
    
    let runTest rsp (t : DTestCase2) = 
        async { 
            let teSearchPath =
                rsp.SolutionPaths.Path
                |> FilePath.getDirectoryName
                |> Prelude.flip FilePath.combine (FilePath "packages")

            let tr =
                t
                |> Seq.singleton
                |> TestAdapterExtensions.executeTest teSearchPath 
                |> Seq.head
            
            return tr
        }
