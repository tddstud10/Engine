module R4nd0mApps.TddStud10.Engine.Actors.IdeEvents

open ActorMessages
open Akka.Event
open Akka.FSharp

type RunProgress = 
    { TotalProjects : int
      BuildsDone : int
      InsturmentationDone : int
      DiscoveryDone : int
      TotalTests : int
      TestsDone : int }
    
    static member Empty = 
        { TotalProjects = 0
          BuildsDone = 0
          InsturmentationDone = 0
          DiscoveryDone = 0
          TotalTests = 0
          TestsDone = 0 }
    
    override it.ToString() = 
        sprintf "Ps = %d, B = %d, I = %d, D = %d | Ts = %d, T = %d" it.TotalProjects it.BuildsDone 
            it.InsturmentationDone it.DiscoveryDone it.TotalTests it.TestsDone

let actorLoop (m : Actor<_>) = 
    m.Context.System.EventStream.Subscribe<IdeEventsMessage>(m.Self) |> ignore
    let rec loop rp = 
        actor { 
            let! msg = m.Receive()
            let msg, rp = 
                match msg with
                | EvResyncStarting id -> sprintf "[%O] Resync started" id, Some RunProgress.Empty
                | EvRunStarting id -> sprintf "[%O] Run started" id, Some RunProgress.Empty
                | EvProjectsDiscovered(id, ps) -> 
                    sprintf "[%O] Projects Discovered: %O" id ps, 
                    rp |> Option.bind (fun rp -> { rp with TotalProjects = ps.Length } |> Some)
                | EvProjectSnapshotStarting(id, p) -> sprintf "[%O] Project snapshot starting: %O" id p, rp
                | EvProjectSnapshotSucceeded(id, p) -> sprintf "[%O]     Project snapshot succeeded: %O" id p, rp
                | EvProjectSnapshotFailed(id, p, e) -> sprintf "[%O]     Project snapshot failed: %O %A" id p e, rp
                | EvProjectFileFixStarting(id, p) -> sprintf "[%O] Project file fix starting: %O" id p, rp
                | EvProjectFileFixSucceeded(id, p) -> sprintf "[%O]     Project file fix succeeded: %O" id p, rp
                | EvProjectFileFixFailed(id, p, e) -> sprintf "[%O]     Project file fix failed: %O %A" id p e, rp
                | EvProjectBuildStarting(id, p) -> sprintf "[%O] Project build starting: %O" id p, rp
                | EvProjectBuildSucceeded(id, p) -> 
                    sprintf "[%O]     Project build succeeded: %O" id p, 
                    rp |> Option.bind (fun rp -> { rp with BuildsDone = rp.BuildsDone + 1 } |> Some)
                | EvProjectBuildFailed(id, p, e) -> 
                    sprintf "[%O]     Project build failed: %O %A" id p e, 
                    rp |> Option.bind (fun rp -> { rp with BuildsDone = rp.BuildsDone + 1 } |> Some)
                | EvAssemblyInstrumentationStarting(id, a) -> 
                    sprintf "[%O] Assembly instrumentation starting: %O" id a, rp
                | EvAssemblyInstrumentationSucceeded(id, a) -> 
                    sprintf "[%O]     Assembly instrumentation succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with InsturmentationDone = rp.InsturmentationDone + 1 } |> Some)
                | EvAssemblyInstrumentationFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly instrumentation failed: %O %A" id a e, 
                    rp |> Option.bind (fun rp -> { rp with InsturmentationDone = rp.InsturmentationDone + 1 } |> Some)
                | EvAssemblySequencePointsDiscoveryStarting(id, a) -> 
                    sprintf "[%O] Assembly sequence points discovery starting: %O" id a, rp
                | EvSequencePointsDiscovered(id, t) -> 
                    sprintf "[%O] Sequence Points Discovered: %O" id t, 
                    rp |> Option.bind (fun rp -> { rp with TotalTests = rp.TotalTests + 1 } |> Some)
                | EvAssemblySequencePointsDiscoverySucceeded(id, a) -> 
                    sprintf "[%O]     Assembly sequence points discovery succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with DiscoveryDone = rp.DiscoveryDone + 1 } |> Some)
                | EvAssemblySequencePointsDiscoveryFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly sequence points discovery failed: %O %A" id a e, 
                    rp |> Option.bind (fun rp -> { rp with DiscoveryDone = rp.DiscoveryDone + 1 } |> Some)
                | EvAssemblyTestDiscoveryStarting(id, a) -> sprintf "[%O] Assembly test discovery starting: %O" id a, rp
                | EvTestDiscovered(id, t) -> 
                    sprintf "[%O] Tests Discovered: %O" id t, 
                    rp |> Option.bind (fun rp -> { rp with TotalTests = rp.TotalTests + 1 } |> Some)
                | EvAssemblyTestDiscoverySucceeded(id, a) -> 
                    sprintf "[%O]     Assembly test discovery succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with DiscoveryDone = rp.DiscoveryDone + 1 } |> Some)
                | EvAssemblyTestDiscoveryFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly test discovery failed: %O %A" id a e, 
                    rp |> Option.bind (fun rp -> { rp with DiscoveryDone = rp.DiscoveryDone + 1 } |> Some)
                | EvTestRunStarting(id, t) -> sprintf "[%O] Test run starting: %O" id t, rp
                | EvTestRunSucceeded(id, t) -> 
                    sprintf "[%O]     Test run succeeded: %O" id t, 
                    rp |> Option.bind (fun rp -> { rp with TestsDone = rp.TestsDone + 1 } |> Some)
                | EvTestRunFailed(id, t, e) -> 
                    sprintf "[%O]     Test run failed: %O %A" id t e, 
                    rp |> Option.bind (fun rp -> { rp with TestsDone = rp.TestsDone + 1 } |> Some)
            System.Console.WriteLine(sprintf "%s [%O]" msg rp)
            return! loop rp
        }
    loop None
