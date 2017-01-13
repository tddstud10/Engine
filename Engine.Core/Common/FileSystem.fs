namespace R4nd0mApps.TddStud10.Common

open System
open System.IO
open System.Reactive.Linq
open System.Reflection

module FileSystem = 
    type FileSystemEvent = 
        | Created of FileInfo
        | Changed of FileInfo
        | Deleted of FileInfo
        | Renamed of FileInfo * FileInfo
    
    let watch nf so filter path = 
        Observable.Create<_>(fun (observer : IObserver<_>) -> 
            let fsw = new FileSystemWatcher(path, filter)
            fsw.Created.AddHandler(fun _ args -> observer.OnNext(Created(FileInfo(args.FullPath))))
            fsw.Changed.AddHandler(fun _ args -> observer.OnNext(Changed(FileInfo(args.FullPath))))
            fsw.Deleted.AddHandler(fun _ args -> observer.OnNext(Deleted(FileInfo(args.FullPath))))
            fsw.Renamed.AddHandler
                (fun _ args -> observer.OnNext(Renamed(FileInfo(args.FullPath), FileInfo(args.OldFullPath))))
            fsw.Error.AddHandler(fun _ args -> observer.OnError(args.GetException()))
            fsw.EnableRaisingEvents <- true
            fsw.IncludeSubdirectories <- (so = SearchOption.AllDirectories)
            nf |> Option.fold (fun () e -> fsw.NotifyFilter <- e) ()
            fsw :> IDisposable)

module Assembly = 
    let getAssemblyLocation (asm : Assembly) = 
        (Uri(asm.CodeBase)).LocalPath
        |> Path.GetFullPath
        |> Path.GetDirectoryName
