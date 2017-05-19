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

let private projectIDFromMatch slnDir (m : Match) = 
    let projectFilePath = 
        m.Groups.["path"].Value
        |> Path.combine slnDir

    { Name = m.Groups.["name"].Value
      FileName = projectFilePath |> Path.GetFileName |> FilePath
      DirectoryName = projectFilePath |> Path.GetDirectoryName |> FilePath
      Id = m.Groups.["guid"].Value |> Guid
      Type = m.Groups.["typeGuid"].Value |> Guid }

let private getListOfProjectItems excludePatterns (FilePath projectPath) = 
    projectPath
    |> Path.GetDirectoryName
    |> fun d -> Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories)
    |> Seq.filter (fun p -> 
           excludePatterns
           |> Array.exists (fun ep -> p.IndexOf(ep, 0, StringComparison.OrdinalIgnoreCase) >= 0)
           |> not)
    |> Seq.map FilePath
    |> Seq.toArray

let private getProjectReferences (FilePath projectPath) = 
    let doc = XmlDocument()
    doc.Load(projectPath)
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    nsmgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
    doc.DocumentElement.SelectNodes("//msb:ProjectReference", nsmgr)
    |> Seq.cast<XmlNode>
    |> Seq.map (fun xn -> Option.attempt (fun () -> xn.Attributes.["Include"].Value))
    |> Seq.choose id
    |> Seq.map (Path.combine (Path.GetDirectoryName(projectPath))
                >> Path.GetFullPath
                >> FilePath)
    
let private findProjectIDGivenPath projects projectPath = 
    projects
    |> List.tryFind (fun p -> p.Id.Path = projectPath)
    |> Option.map (fun p -> p.Id)

let load excludePatterns (slnPath : FilePath) = 
    let slnDir = slnPath.ToString() |> Path.GetDirectoryName
    
    let projects = 
        slnPath.ToString()
        |> File.ReadAllText
        |> projectPattern.Match
        |> getAllMatches []
        |> List.map (projectIDFromMatch slnDir)
        |> List.filter (fun pid -> Set.contains pid.Type supportedProjectTypes)
        |> List.map (fun pid -> 
               { Id = pid
                 Items = getListOfProjectItems excludePatterns pid.Path })
    
    let dependencyMap = 
        projects
        |> List.map (fun p -> 
               (p.Id, 
                p.Id.Path
                |> getProjectReferences
                |> Seq.choose (findProjectIDGivenPath projects)))
        |> dict
        |> ReadOnlyDictionary<_, _>
    
    { Path = slnPath
      Projects = projects |> Array.ofList
      DependencyMap = dependencyMap }

let createDGraph sln = 
    Graph.create (sln.Projects
                  |> Array.map (fun p -> p.Id, p.Id.ToString())
                  |> Array.toList) (sln.DependencyMap
                                    |> Seq.map 
                                           (fun kvp -> 
                                           kvp.Value |> Seq.map (fun p2 -> kvp.Key, p2, sprintf "%O -> %O" kvp.Key p2))
                                    |> Seq.collect id
                                    |> Seq.toList)
