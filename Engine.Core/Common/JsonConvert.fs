module R4nd0mApps.TddStud10.Common.JsonConvert

open Newtonsoft.Json
open R4nd0mApps.TddStud10.Common.Domain
open System.Collections.Concurrent

type ConcurrentBagJsonConverter<'T>() = 
    inherit JsonConverter()
    override __.CanConvert(objectType) = typeof<ConcurrentBag<'T>>.IsAssignableFrom(objectType)
    
    override __.ReadJson(reader, _, _, serializer) = 
        let list = ResizeArray<'T>()
        serializer.Populate(reader, list)
        ConcurrentBag<'T>(list) :> _
    
    override __.WriteJson(_, _, _) = failwith "not implemented"

let private jcs = 
    [| ConcurrentBagJsonConverter<DTestCase>() :> JsonConverter
       ConcurrentBagJsonConverter<TestFailureInfo>() :> _
       ConcurrentBagJsonConverter<SequencePoint>() :> _
       ConcurrentBagJsonConverter<TestRunId>() :> _
       ConcurrentBagJsonConverter<DTestResult>() :> _ |]

let private cfg = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
let deserialize<'a> s = JsonConvert.DeserializeObject<'a>(s, jcs)
let serialize o = JsonConvert.SerializeObject(o, cfg)
let serialize2 o = JsonConvert.SerializeObject(o, Formatting.Indented, cfg)
