module R4nd0mApps.TddStud10.Engine.RunSteps.ProjectFileFixer

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System.IO

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
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
let fixProjectFile _ p = async { return p }
