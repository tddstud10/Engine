module R4nd0mApps.TddStud10.Engine.Core.DataStoreTests

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.TestFramework
open System
open System.Collections.Concurrent
open Xunit

let createDS slnPath = 
    let ds = DataStore() :> IDataStore
    RunStartParams.Create (EngineConfig()) DateTime.Now (FilePath slnPath) |> ds.SetRunStartParams
    ds

let createDSWithPATC slnPath = 
    let ds = createDS slnPath
    let spy = CallSpy<PerDocumentLocationDTestCases>(Throws(Exception()))
    ds.TestCasesUpdated.Add(spy.Func >> ignore)
    ds, spy

let createDSWithPDSP slnPath = 
    let ds = createDS slnPath
    let spy = CallSpy<PerDocumentSequencePoints>(Throws(Exception()))
    ds.SequencePointsUpdated.Add(spy.Func >> ignore)
    ds, spy

let createDSWithTRO slnPath = 
    let ds = createDS slnPath
    let spy1 = CallSpy<PerTestIdDResults>(Throws(Exception()))
    ds.TestResultsUpdated.Add(spy1.Func >> ignore)
    let spy2 = CallSpy<PerDocumentLocationTestFailureInfo>(Throws(Exception()))
    ds.TestFailureInfoUpdated.Add(spy2.Func >> ignore)
    let spy3 = CallSpy<PerSequencePointIdTestRunId>(Throws(Exception()))
    ds.CoverageInfoUpdated.Add(spy3.Func >> ignore)
    ds, spy1, spy2, spy3

let createDSWithAllItems slnPath = 
    let ds = createDS slnPath
    let spy1 = CallSpy<PerTestIdDResults>(Throws(Exception()))
    ds.TestResultsUpdated.Add(spy1.Func >> ignore)
    let spy2 = CallSpy<PerDocumentLocationTestFailureInfo>(Throws(Exception()))
    ds.TestFailureInfoUpdated.Add(spy2.Func >> ignore)
    let spy3 = CallSpy<PerSequencePointIdTestRunId>(Throws(Exception()))
    ds.CoverageInfoUpdated.Add(spy3.Func >> ignore)
    let spy4 = CallSpy<PerDocumentLocationDTestCases>(Throws(Exception()))
    ds.TestCasesUpdated.Add(spy4.Func >> ignore)
    let spy5 = CallSpy<PerDocumentSequencePoints>(Throws(Exception()))
    ds.SequencePointsUpdated.Add(spy5.Func >> ignore)
    ds, spy1, spy2, spy3, spy4, spy5

let createPDLTC (ts : (string * FilePath * FilePath * DocumentCoordinate) list) = 
    let patc = PerDocumentLocationDTestCases()
    
    let addTestCase (acc : PerDocumentLocationDTestCases) (f, s, d, l) = 
        let tc = 
            { DtcId = Guid()
              FullyQualifiedName = f
              DisplayName = ""
              Source = s
              CodeFilePath = d
              LineNumber = l }
        
        let b = 
            acc.GetOrAdd({ document = d
                           line = l }, fun _ -> ConcurrentBag<_>())
        
        b.Add(tc) |> ignore
        acc
    ts |> Seq.fold addTestCase patc

let createPDSP() = PerDocumentSequencePoints()
let createTRO() = PerTestIdDResults(), PerDocumentLocationTestFailureInfo(), PerSequencePointIdTestRunId()

[<Fact>]
let ``UpdateData with PATV causes event to be fired and crash in handler is ignored``() = 
    let ds, spy = createDSWithPATC @"c:\a.sln"
    let patc = [] |> createPDLTC
    ds.UpdateData(patc |> TestCases)
    Assert.Equal(spy.CalledWith, Some patc)

[<Fact>]
let ``UpdateData with PDSP causes event to be fired and crash in handler is ignored``() = 
    let ds, spy = createDSWithPDSP @"c:\a.sln"
    let pdsp = () |> createPDSP
    ds.UpdateData(pdsp |> SequencePoints)
    Assert.Equal(spy.CalledWith, Some pdsp)

[<Fact>]
let ``UpdateData with TRO causes event to be fired and crash in handler is ignored``() = 
    let ds, spy1, spy2, spy3 = createDSWithTRO @"c:\a.sln"
    let ptir, pdtfi, paspc = () |> createTRO
    ds.UpdateData((ptir, pdtfi, paspc) |> TestRunOutput)
    Assert.Equal(spy1.CalledWith, Some ptir)
    Assert.Equal(spy2.CalledWith, Some pdtfi)
    Assert.Equal(spy3.CalledWith, Some paspc)

[<Fact>]
let ``ResetData resets all data``() = 
    let ds, spy1, spy2, spy3, spy4, spy5 = createDSWithAllItems @"c:\a.sln"
    let _, _ = createDSWithPATC @"c:\a.sln"
    let ptir, pdtfi, paspc = () |> createTRO
    let pdsp = () |> createPDSP
    let patc = [] |> createPDLTC
    ds.UpdateData((ptir, pdtfi, paspc) |> TestRunOutput)
    ds.UpdateData(pdsp |> SequencePoints)
    ds.UpdateData(patc |> TestCases)
    ds.ResetData()
    Assert.Equal(spy1.CalledWith.Value.Count, 0)
    Assert.Equal(spy2.CalledWith.Value.Count, 0)
    Assert.Equal(spy3.CalledWith.Value.Count, 0)
    Assert.Equal(spy4.CalledWith.Value.Count, 0)
    Assert.Equal(spy5.CalledWith.Value.Count, 0)

let fp = FilePath
let dc = DocumentCoordinate

[<Fact>]
let ``FindTestsInFile - Returns empty when no tests exist in file``() = 
    let ds = createDS @"c:\a.sln"
    [ "FQN1", fp "a.dll", fp @"c:\a\b.cpp", dc 10 ]
    |> createPDLTC
    |> TestCases
    |> ds.UpdateData
    let ts = fp @"c:\a\c.cpp" |> ds.FindTestsInFile
    Assert.Equal(0, ts.Keys.Count)

[<Fact>]
let ``FindTestsInFile - Returns tests across all document locations``() = 
    let ds = createDS @"c:\a.sln"
    [ "FQN1", fp "a.dll", fp @"c:\a\1.cpp", dc 100
      "FQN2", fp "b.dll", fp @"c:\a\1.cpp", dc 200
      "FQN3", fp "a.dll", fp @"c:\a\2.cpp", dc 100 ]
    |> createPDLTC
    |> TestCases
    |> ds.UpdateData
    let ts = fp @"c:\a\1.cpp" |> ds.FindTestsInFile
    Assert.Equal(2, ts.Keys.Count)
    Assert.Equal<string []>([| "FQN1"; "FQN2" |], 
                            ts.Values
                            |> Seq.collect id
                            |> Seq.map (fun t -> t.FullyQualifiedName)
                            |> Seq.sort
                            |> Seq.toArray)

let createPDLTFI (data : (string * FilePath * DocumentCoordinate) list) = 
    let pdltfi = PerDocumentLocationTestFailureInfo()
    
    let addTFI (acc : PerDocumentLocationTestFailureInfo) (m, d, l) = 
        let tc = 
            { message = m
              stack = [||] }
        
        let b = 
            acc.GetOrAdd({ document = d
                           line = l }, fun _ -> ConcurrentBag<_>())
        
        b.Add(tc) |> ignore
        acc
    data |> Seq.fold addTFI pdltfi

[<Fact>]
let ``FindTestFailureInfosInFile - Returns empty when no tests exist in file``() = 
    let ds = createDS @"c:\a.sln"
    [ "Message 1", fp @"c:\a\b.cpp", dc 10 ]
    |> createPDLTFI
    |> fun it -> (PerTestIdDResults(), it, PerSequencePointIdTestRunId()) |> TestRunOutput
    |> ds.UpdateData
    let ts = fp @"c:\a\c.cpp" |> ds.FindTestFailureInfosInFile
    Assert.Equal(0, ts.Keys.Count)

[<Fact>]
let ``FindTestFailureInfosInFile - Returns tests across all document locations``() = 
    let ds = createDS @"c:\a.sln"
    [ "Message 1", fp @"c:\a\1.cpp", dc 100
      "Message 2", fp @"c:\a\1.cpp", dc 200
      "Message 3", fp @"c:\a\2.cpp", dc 100 ]
    |> createPDLTFI
    |> fun it -> (PerTestIdDResults(), it, PerSequencePointIdTestRunId()) |> TestRunOutput
    |> ds.UpdateData
    let ts = fp @"c:\a\1.cpp" |> ds.FindTestFailureInfosInFile
    Assert.Equal(2, ts.Keys.Count)
    Assert.Equal<string []>([| "Message 1"; "Message 2" |], 
                            ts.Values
                            |> Seq.collect id
                            |> Seq.map (fun t -> t.message)
                            |> Seq.sort
                            |> Seq.toArray)

type SimpleTestCase = 
    { fqn : string
      src : FilePath
      file : FilePath
      ln : DocumentCoordinate }
    
    member self.ToTC() = 
        { DtcId = Guid()
          FullyQualifiedName = self.fqn
          DisplayName = ""
          Source = self.src
          CodeFilePath = self.file
          LineNumber = self.ln }
    
    member self.ToTID() = 
        { source = self.src
          location = 
              { document = self.file
                line = self.ln } }
    
    static member FromTC(tc : DTestCase) = 
        { fqn = tc.FullyQualifiedName
          src = tc.Source
          file = tc.CodeFilePath
          ln = tc.LineNumber }

type SimpleTestResult = 
    { name : string
      outcome : DTestOutcome }
    
    member self.ToTR tc = 
        { DisplayName = self.name
          TestCase = tc
          Outcome = self.outcome
          ErrorMessage = ""
          ErrorStackTrace = "" }
    
    static member FromTR(tr : DTestResult) = 
        { name = tr.DisplayName
          outcome = tr.Outcome }

let createTestRunOutput (data : list<SequencePointId * SimpleTestCase>) 
    (data2 : list<SimpleTestCase * SimpleTestResult>) = 
    let pspiri = PerSequencePointIdTestRunId()
    data |> Seq.iter (fun (spid, tc) -> 
                let b = pspiri.GetOrAdd(spid, fun _ -> ConcurrentBag<_>())
                
                let trid = 
                    { testId = tc.ToTID()
                      testRunInstanceId = TestRunInstanceId(obj().GetHashCode()) }
                b.Add(trid))
    let ptir = PerTestIdDResults()
    data2 |> List.iter (fun (tc, tr) -> 
                 let b = ptir.GetOrAdd(tc.ToTID(), fun _ -> ConcurrentBag<_>())
                 b.Add(tc.ToTC() |> tr.ToTR))
    (ptir, PerDocumentLocationTestFailureInfo(), pspiri)

let stubSpid1 = 
    { methodId = 
          { assemblyId = AssemblyId(Guid.NewGuid())
            mdTokenRid = MdTokenRid 101u }
      uid = 1 }

let stubSpid2 = 
    { methodId = 
          { assemblyId = AssemblyId(Guid.NewGuid())
            mdTokenRid = MdTokenRid 102u }
      uid = 2 }

let stubTR1 = 
    { name = "Test Result #1"
      outcome = DTestOutcome.TOPassed }

let stubTR2 = 
    { name = "Test Result #2"
      outcome = DTestOutcome.TOFailed }

let stubTC1 = 
    { fqn = "FQNTest#1"
      src = FilePath "testdll1.dll"
      file = FilePath "test1.cpp"
      ln = DocumentCoordinate 100 }

let stubTC2 = 
    { fqn = "FQNTest#2"
      src = FilePath "testdll2.dll"
      file = FilePath "test2.cpp"
      ln = DocumentCoordinate 200 }

[<Fact>]
let ``GetTestResultsForSequencepointsIds - Returns empty when no coverage info``() = 
    let ds = createDS @"c:\a.sln"
    ([], [])
    ||> createTestRunOutput
    |> TestRunOutput
    |> ds.UpdateData
    let ret = [ stubSpid1 ] |> ds.GetTestResultsForSequencepointsIds
    Assert.Equal(1, ret.Keys.Count)
    Assert.Equal(0, ret.[stubSpid1].Length)

[<Fact>]
let ``GetTestResultsForSequencepointsIds - Returns results for each sequence point with dups removed``() = 
    let dtrsToStr = Array.map (fun (tr : DTestResult) -> sprintf "%s - %A" tr.DisplayName tr.Outcome) >> Array.sort
    let ds = createDS @"c:\a.sln"
    ([ stubSpid1, stubTC1
       stubSpid1, stubTC1
       stubSpid1, stubTC2
       stubSpid2, stubTC1 ], 
     [ stubTC1, stubTR1
       stubTC2, stubTR2 ])
    ||> createTestRunOutput
    |> TestRunOutput
    |> ds.UpdateData
    let ret = [ stubSpid1; stubSpid2 ] |> ds.GetTestResultsForSequencepointsIds
    Assert.Equal(2, ret.Keys.Count)
    Assert.Equal<string []>([| "Test Result #1 - TOPassed"; "Test Result #2 - TOFailed" |], 
                            ret.[stubSpid1] |> dtrsToStr)
    Assert.Equal<string []>([| "Test Result #1 - TOPassed" |], 
                            ret.[stubSpid2] |> dtrsToStr)
