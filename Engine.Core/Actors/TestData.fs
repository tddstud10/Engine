module R4nd0mApps.TddStud10.Engine.Actors.TestData

open System
open System.IO
open System.Diagnostics
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
            
[<DebuggerDisplay("{AsString}")>]
[<StructuredFormatDisplay("{AsString}")>]
type TestId = 
    { TestName : string
      BuildOutput : ProjectBuilderOutput }
    member it.AsString = sprintf "Test: %O [%O]" it.TestName it.BuildOutput

type FailureInfo =
    { Message : string
      StackTrace : string }
    static member FromException (e : exn) =
        { Message = e.Message 
          StackTrace = e.StackTrace }

type RunId = Guid

// ---------------------------------------------------------------------------------------------------------

let inline (~~) x = FilePath x

let Tests =
    [ ] |> Map.ofList

module API =
    open Microsoft.Build.Execution
    open Microsoft.Build.Framework
    open System.Collections.Generic
    open System.Collections.Concurrent
    open System.Text
    open System.Xml.Linq
    open System.Xml

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
            let snapshotPath = 
                rsp.SolutionPaths.SnapshotPath 
                |> FilePath.getDirectoryName
                |> Prelude.flip FilePath.combine (p.Index.ToString() |> FilePath)
            let! _ = p.Items
                     |> Array.map (updateProjectItemSnapshot snapshotPath)
                     |> Async.Parallel
            return { ProjectSnapshotCreatorOutput.SnapshotPath = snapshotPath; Project = p }
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
    let fixProjectFile rsp p =
        async {
            do! Async.Sleep(1000)
            return p
        }

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

    let buildProject rsp (sso: ProjectSnapshotCreatorOutput) =
        async {
            let wrapperProjectPath = 
                (sso.SnapshotPath, wrapperProjectName |> FilePath) 
                ||> FilePath.combine
            FilePath.writeAllText wrapperProjectContents wrapperProjectPath

            let props = 
                [ "_TddStud10Project", (FilePath.combine sso.SnapshotPath sso.Project.FileName).ToString()
                  "_TddStud10Target", "Build"
                  "Configuration", "Debug" 
                  "CreateVsixContainer", "false" 
                  "DeployExtension", "false" 
                  "CopyVsixExtensionFiles", "false" 
                  "RunCodeAnalysis", "false" ]
                |> Map.ofList

            let props =
                rsp.Config.AdditionalMSBuildProperties
                |> Array.map (String.split '=' >>
                             function
                             | [p; v] -> Some (p, v)
                             | _ -> None)
                |> Array.choose id
                |> Array.fold (fun acc (p, v) -> Map.add p v acc) props

            let props = props :> IDictionary<_, _>
        
            let l = BuildLogger()
            let proj = ProjectInstance(wrapperProjectPath.ToString(), props, "14.0")
            let status = proj.Build([| "_TddStud10BuildProject" |], [ l :> ILogger ])
            let outputs = proj.GetItems("_TddStud10TargetOutputs") |> Seq.map (fun i -> i.EvaluatedInclude |> FilePath)
            
            if not status then
                l.Warnings 
                |> Seq.append l.Errors
                |> Seq.fold (fun (acc: StringBuilder) -> acc.AppendLine) (StringBuilder())
                |> failwithf "Build Errors %O" 
            return { Items = outputs |> Array.ofSeq; SnapshotPath = sso.SnapshotPath; Project = sso.Project }
        }

    let instrumentAssembly rid a =
        async {
            do! Async.Sleep(1000)
            return a
        }

    let discoverAssemblySequencePoints f rid (pbo: ProjectBuilderOutput) =
        async {
            do! Async.Sleep(1000)
            Tests 
            |> Map.tryFind pbo.Items.[0]
            |> Option.fold (fun _ x -> x) []
            |> List.map (fun t -> rid, { TestName = t; BuildOutput = pbo })
            |> List.iter f
        }

    let discoverAssemblyTests f rid a =
        async {
            do! Async.Sleep(1000)
            Tests 
            |> Map.tryFind a.Items.[0]
            |> Option.fold (fun _ x -> x) []
            |> List.map (fun t -> rid, { TestName = t; BuildOutput = a })
            |> List.iter f
        }

    let runTest rid t =
        async {
            if t.TestName = "Test 1" then
                failwithf "Test failed"

            do! Async.Sleep(1000)
        }

