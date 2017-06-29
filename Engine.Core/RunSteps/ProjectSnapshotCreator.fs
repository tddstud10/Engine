module R4nd0mApps.TddStud10.Engine.RunSteps.ProjectSnapshotCreator

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.Core
open System.IO

let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

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
            [ rsp.SolutionPaths.SnapshotPath |> FilePath.getDirectoryName
              p.Index.ToString() |> FilePath ]
            |> FilePath.combine
        let! _ = p.Items
                 |> Array.map (updateProjectItemSnapshot snapshotDir)
                 |> Async.Parallel
        return { ProjectSnapshotCreatorOutput.SnapshotDirectoryName = snapshotDir
                 Project = p }
    }
