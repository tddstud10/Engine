module R4nd0mApps.TddStud10.Engine.Core.SolutionLoader

open Hekate
open R4nd0mApps.TddStud10.Common.Domain
open System
open System.Collections.ObjectModel
open System.IO
open System.Text.RegularExpressions
open System.Xml

let private projectPattern = 
    Regex
        (@"Project\(\""(?<typeGuid>.*?)\""\)\s+=\s+\""(?<name>.*?)\"",\s+\""(?<path>.*?)\"",\s+\""(?<guid>.*?)\""(?<content>.*?)\bEndProject\b", 
         RegexOptions.ExplicitCapture ||| RegexOptions.Singleline ||| RegexOptions.Compiled)

let private supportedProjectTypes = 
    [ Guid("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
      Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")
      Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") ]
    |> Set.ofList

let rec private getAllMatches acc (m : Match) = 
    if not m.Success then acc
    else getAllMatches (m :: acc) (m.NextMatch())

let private getProjectItems excludePatterns projectPath = 
    let projDir =  
        projectPath
        |> FilePath.getDirectoryName

    projDir
    |> FilePath.enumerateFiles SearchOption.AllDirectories "*"
    |> Seq.filter (fun p -> 
           excludePatterns
           |> Array.exists (fun ep -> p.ToString().IndexOf(ep, 0, StringComparison.OrdinalIgnoreCase) >= 0)
           |> not)
    |> Seq.map (FilePath.makeRelativePath projDir)
    |> Seq.toArray

let private projectIDFromMatch excludePatterns slnDir i (m : Match) = 
    let typeGuid = m.Groups.["typeGuid"].Value |> Guid
    if not <| Set.contains typeGuid supportedProjectTypes then
        None
    else
        let projectFilePath = 
            [ slnDir; FilePath m.Groups.["path"].Value ] 
            |> FilePath.combine

        { Index = i
          Name = m.Groups.["name"].Value
          FileName = projectFilePath |> FilePath.getFileName
          DirectoryName = projectFilePath |> FilePath.getDirectoryName
          Id = m.Groups.["guid"].Value |> Guid
          Type = m.Groups.["typeGuid"].Value |> Guid
          Items = getProjectItems excludePatterns projectFilePath } |> Some


let private getProjectReferences projectPath = 
    let doc = XmlDocument()
    doc.Load(projectPath.ToString())
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    nsmgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
    doc.DocumentElement.SelectNodes("//msb:ProjectReference", nsmgr)
    |> Seq.cast<XmlNode>
    |> Seq.map (fun xn -> Option.attempt (fun () -> xn.Attributes.["Include"].Value |> FilePath))
    |> Seq.choose id
    |> Seq.map (fun p -> [ FilePath.getDirectoryName projectPath; p] |> FilePath.combine |> FilePath.getFullPath)
    
let private findProjectFromPath (projects : Project list) projectPath = 
    projects
    |> List.tryFind (fun p -> p.Path = projectPath)

let load excludePatterns slnPath = 
    let slnDir = slnPath |> FilePath.getDirectoryName
    
    let projects = 
        slnPath
        |> FilePath.readAllText
        |> projectPattern.Match
        |> getAllMatches []
        |> List.mapi (projectIDFromMatch excludePatterns slnDir)
        |> List.choose id
    
    let dependencyMap = 
        projects
        |> List.map (fun p -> 
               (p, 
                p.Path
                |> getProjectReferences
                |> Seq.choose (findProjectFromPath projects)))
        |> dict
        |> ReadOnlyDictionary<_, _>
    
    { Path = slnPath
      Projects = projects |> Array.ofList
      DependencyMap = dependencyMap }

let createDGraph sln = 
    Graph.create (sln.Projects
                  |> Array.map (fun p -> p, p.Id.ToString())
                  |> Array.toList) (sln.DependencyMap
                                    |> Seq.map 
                                           (fun kvp -> 
                                           kvp.Value |> Seq.map (fun p2 -> kvp.Key, p2, sprintf "%O -> %O" kvp.Key.Id p2.Id))
                                    |> Seq.collect id
                                    |> Seq.toList)
