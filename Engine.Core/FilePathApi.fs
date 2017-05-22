namespace R4nd0mApps.TddStud10.Common.Domain

// TODO: Move this to Common
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FilePath = 
    open System
    open System.IO
    
    let getDirectoryName (FilePath p) = 
        p
        |> Path.GetDirectoryName
        |> FilePath
    
    let getFullPath (FilePath p) = 
        p
        |> Path.GetFullPath
        |> FilePath

    let getFileName (FilePath p) = 
        p
        |> Path.GetFileName
        |> FilePath
    
    let getFileNameWithoutExtension (FilePath p) = 
        p
        |> Path.GetFileName
        |> FilePath
    
    let getPathWithoutRoot (FilePath p) = p.Substring(Path.GetPathRoot(p).Length) |> FilePath
    let combine (FilePath p1) (FilePath p2) = Path.Combine(p1, p2) |> FilePath
    let createDirectory (FilePath p) = p |> Directory.CreateDirectory
    let writeAllText text (FilePath p) = (p, text) |> File.WriteAllText
    let readAllText (FilePath p) = p |> File.ReadAllText
    let enumerateFiles so pattern (FilePath path) = 
        Directory.EnumerateFiles(path, pattern, so)
        |> Seq.map FilePath
    
    let makeRelativePath (FilePath folder) (FilePath file) = 
        let file = Uri(file, UriKind.Absolute)
        
        let folder = 
            if folder.EndsWith(@"\") then folder
            else folder + @"\"
        
        let folder = Uri(folder, UriKind.Absolute)
        folder.MakeRelativeUri(file).ToString().Replace('/', Path.DirectorySeparatorChar)
        |> Uri.UnescapeDataString
        |> FilePath
