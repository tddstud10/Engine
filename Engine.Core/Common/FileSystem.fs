namespace R4nd0mApps.TddStud10.Common

open System
open System.IO
open System.Reactive.Linq

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

module RemoteEvents = 
    open FileSystem
    open Newtonsoft.Json
    
    type Type = 
        | Source
        | Sink
    
    let private prepareNs ns = 
        if String.IsNullOrWhiteSpace(ns) then invalidArg ns "Namespace cannot be empty or null"
        let path = Path.Combine(Path.GetTempPath(), "remoteevents", ns)
        if path
           |> Directory.Exists
           |> not
        then Directory.CreateDirectory(path) |> ignore
        path
    
    let private prepareSinkEvent<'T> ns eventName = 
        let path = prepareNs ns
        let e = Event<'T>()
        let filter = NotifyFilters.LastWrite ||| NotifyFilters.Size |> Some
        
        let sub = 
            FileSystem.watch filter SearchOption.TopDirectoryOnly (sprintf "%s_*" eventName) path
            |> Observable.choose (function 
                   | Changed fi -> Some fi
                   | _ -> None)
            |> Observable.Distinct
            |> Observable.subscribe (fun fi -> 
                   let content = File.ReadAllText(fi.FullName)
                   Exec.safeExec (fun () -> File.Delete(fi.FullName))
                   e.Trigger(JsonConvert.DeserializeObject<'T>(content)))
        e, sub
    
    let private prepareSourceEvent<'T> ns eventName = 
        let path = prepareNs ns
        let e = Event<'T>()
        
        let sub = 
            e.Publish |> Observable.subscribe (fun obj -> 
                             let s = JsonConvert.SerializeObject(obj)
                             let fName = sprintf "%s_%O" eventName (Guid.NewGuid())
                             File.WriteAllText(Path.Combine(path, fName), s))
        e, sub
    
    let prepareEvent<'T> = 
        function 
        | Source -> prepareSourceEvent<'T>
        | Sink -> prepareSinkEvent<'T>
