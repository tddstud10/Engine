module R4nd0mApps.TddStud10.Engine.Actors.IdeEvents

open ActorMessages
open Akka.Event
open Akka.FSharp

type RunProgress = 
    { TotalProjects : int
      BuildsDone : int
      SequencePointDiscoveryDone : int
      TestDiscoveryDone : int
      TotalTests : int
      TestsDone : int }
    
    static member Empty = 
        { TotalProjects = 0
          BuildsDone = 0
          TestDiscoveryDone = 0
          SequencePointDiscoveryDone = 0
          TotalTests = 0
          TestsDone = 0 }
    
    override it.ToString() = 
        sprintf "Ps = %d | B = %d | D = %d | SPd = %d | Ts = %d, T = %d" it.TotalProjects it.BuildsDone 
            it.TestDiscoveryDone it.SequencePointDiscoveryDone it.TotalTests it.TestsDone

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
                    sprintf "[%O]     Project build succeeded: %O" id p, rp
                | EvProjectBuildFailed(id, p, e) -> 
                    sprintf "[%O]     Project build failed: %O %A" id p e, rp
                | EvAssemblyInstrumentationStarting(id, a) -> 
                    sprintf "[%O] Assembly instrumentation starting: %O" id a, rp
                | EvAssemblyInstrumentationSucceeded(id, a) -> 
                    sprintf "[%O]     Assembly instrumentation succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with BuildsDone = rp.BuildsDone + 1 } |> Some)
                | EvAssemblyInstrumentationFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly instrumentation failed: %O %A" id a e, 
                    rp |> Option.bind (fun rp -> { rp with BuildsDone = rp.BuildsDone + 1 } |> Some)

                | EvAssemblySequencePointsDiscoveryStarting(id, a) -> 
                    sprintf "[%O] Assembly sequence points discovery starting: %O" id a, rp
                | EvSequencePointsDiscovered(id, sps) -> 
                    sprintf "[%O] Sequence Points Discovered: %s" id (sps |> Seq.fold (fun acc e -> acc + sprintf "%O[%d]; " e.Key (Seq.length e.Value)) ""), rp
                | EvAssemblySequencePointsDiscoverySucceeded(id, a) -> 
                    sprintf "[%O]     Assembly sequence points discovery succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with SequencePointDiscoveryDone = rp.SequencePointDiscoveryDone + 1 } |> Some)
                | EvAssemblySequencePointsDiscoveryFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly sequence points discovery failed: %O %A" id a e, 
                    rp |> Option.bind (fun rp -> { rp with SequencePointDiscoveryDone = rp.SequencePointDiscoveryDone + 1 } |> Some)

                | EvAssemblyTestDiscoveryStarting(id, a) -> sprintf "[%O] Assembly test discovery starting: %O" id a, rp
                | EvTestDiscovered(id, t) -> 
                    sprintf "[%O] Tests Discovered: %O" id t.DisplayName, 
                    rp |> Option.bind (fun rp -> { rp with TotalTests = rp.TotalTests + 1 } |> Some)
                | EvAssemblyTestDiscoverySucceeded(id, a) -> 
                    sprintf "[%O]     Assembly test discovery succeeded: %O" id a, 
                    rp |> Option.bind (fun rp -> { rp with TestDiscoveryDone = rp.TestDiscoveryDone + 1 } |> Some)
                | EvAssemblyTestDiscoveryFailed(id, a, e) -> 
                    sprintf "[%O]     Assembly test discovery failed: %O %A" id a e, rp
                | EvTestRunStarting(id, t) -> sprintf "[%O] Test run starting: %O" id t.DisplayName, rp
                | EvTestRunCoverageDataCollected (id, (s, d, l, trid, sps)) -> sprintf "[%O] Coverage data collected: %s#%s#%s[%s]. Count = %d." id s d l trid (Seq.length sps), rp
                | EvTestRunSucceeded(id, t) -> 
                    sprintf "[%O]     Test run succeeded: [%A]%O (Message: %A): Coverage Data Points = %d" id t.Result.Outcome t.Result.DisplayName t.Result.FailureInfo (Seq.length t.CoverageData), 
                    rp |> Option.bind (fun rp -> { rp with TestsDone = rp.TestsDone + 1 } |> Some)
                | EvTestRunFailed(id, t, e) -> 
                    sprintf "[%O]     Test run failed: %O %A" id t.DisplayName e, 
                    rp |> Option.bind (fun rp -> { rp with TestsDone = rp.TestsDone + 1 } |> Some)
            System.Console.WriteLine(sprintf "%s [%O]" msg rp)
            return! loop rp
        }
    loop None
