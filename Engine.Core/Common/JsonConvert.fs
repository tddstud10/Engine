module R4nd0mApps.TddStud10.Common.JsonConvert

open Newtonsoft.Json
open R4nd0mApps.TddStud10.Common.Domain
open System.Collections.Concurrent
open System.ComponentModel

type ConcurrentBagJsonConverter<'T>() = 
    inherit JsonConverter()
    override __.CanConvert(objectType) = typeof<ConcurrentBag<'T>>.IsAssignableFrom(objectType)
    
    override __.ReadJson(reader, objectType, existingValue, serializer) = 
        let list = ResizeArray<'T>()
        serializer.Populate(reader, list)
        ConcurrentBag<'T>(list) :> _
    
    override __.WriteJson(_, _, _) = failwith "not implemented"

type DomainTypeConverter<'T>(fromStr : string -> 'T, toStr : 'T -> string) = 
    inherit TypeConverter()
    
    override __.CanConvertFrom(context, sourceType) = 
        if sourceType = typeof<string> then true
        else base.CanConvertFrom(context, sourceType)
    
    override __.ConvertFrom(context, culture, value) = 
        if value.GetType() = typeof<string> then value :?> string |> fromStr :> _
        else base.ConvertFrom(context, culture, value)
    
    override __.CanConvertTo(context, destinationType) = 
        if destinationType = typeof<string> then true
        else base.CanConvertTo(context, destinationType)
    
    override __.ConvertTo(context, culture, value, destinationType) = 
        if destinationType = typeof<string> then value :?> 'T |> toStr :> _
        else base.ConvertTo(context, culture, value, destinationType)

type FilePathConverter() = 
    inherit DomainTypeConverter<FilePath>(FilePath, fun fp -> fp.ToString())

let dlFromString str = 
    let fields = String.split '|' str
    { document = FilePath fields.[0]
      line = DocumentCoordinate(int fields.[1]) }

let dlToString { document = FilePath d; line = DocumentCoordinate l } = sprintf "%s|%d" d l

type DocumentLocationConverter() = 
    inherit DomainTypeConverter<DocumentLocation>(dlFromString, dlToString)

let tidFromString str = 
    let fields = String.split '|' str
    { source = FilePath fields.[0]
      location = 
          { document = FilePath fields.[1]
            line = DocumentCoordinate(int fields.[2]) } }

let tidToString { source = FilePath s; location = { document = FilePath d; line = DocumentCoordinate l } } = 
    sprintf "%s|%s|%d" s d l

type TestIdConverter() = 
    inherit DomainTypeConverter<TestId>(tidFromString, tidToString)

let spidFromString str = 
    let fields = String.split '|' str
    { methodId = 
          { assemblyId = AssemblyId(System.Guid.Parse fields.[0])
            mdTokenRid = MdTokenRid(uint32 fields.[1]) }
      uid = int fields.[2] }

let spidToString { methodId = { assemblyId = AssemblyId aid; mdTokenRid = MdTokenRid mdtrid }; uid = uid } = 
    sprintf "%O|%u|%d" aid mdtrid uid

type SequencePointIdConverter() = 
    inherit DomainTypeConverter<SequencePointId>(spidFromString, spidToString)

let jcs = 
    [| ConcurrentBagJsonConverter<DTestCase>() :> JsonConverter
       ConcurrentBagJsonConverter<TestFailureInfo>() :> _
       ConcurrentBagJsonConverter<SequencePoint>() :> _
       ConcurrentBagJsonConverter<TestRunId>() :> _
       ConcurrentBagJsonConverter<DTestResult>() :> _ |]

let deserialize<'a> s = 
    do TypeDescriptor.AddAttributes(typeof<FilePath>, TypeConverterAttribute(typeof<FilePathConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<DocumentLocation>, TypeConverterAttribute(typeof<DocumentLocationConverter>)) 
       |> ignore
    do TypeDescriptor.AddAttributes(typeof<TestId>, TypeConverterAttribute(typeof<TestIdConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<SequencePointId>, TypeConverterAttribute(typeof<SequencePointIdConverter>)) 
       |> ignore
    let o = JsonConvert.DeserializeObject<'a>(s, jcs)
    o

let serialize o = 
    let cfg = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    do TypeDescriptor.AddAttributes(typeof<FilePath>, TypeConverterAttribute(typeof<FilePathConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<DocumentLocation>, TypeConverterAttribute(typeof<DocumentLocationConverter>)) 
       |> ignore
    do TypeDescriptor.AddAttributes(typeof<TestId>, TypeConverterAttribute(typeof<TestIdConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<SequencePointId>, TypeConverterAttribute(typeof<SequencePointIdConverter>)) 
       |> ignore
    let s = JsonConvert.SerializeObject(o, cfg)
    s

let serialize2 o = 
    let cfg = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    do TypeDescriptor.AddAttributes(typeof<FilePath>, TypeConverterAttribute(typeof<FilePathConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<DocumentLocation>, TypeConverterAttribute(typeof<DocumentLocationConverter>)) 
       |> ignore
    do TypeDescriptor.AddAttributes(typeof<TestId>, TypeConverterAttribute(typeof<TestIdConverter>)) |> ignore
    do TypeDescriptor.AddAttributes(typeof<SequencePointId>, TypeConverterAttribute(typeof<SequencePointIdConverter>)) 
       |> ignore
    let s = JsonConvert.SerializeObject(o, Formatting.Indented, cfg)
    s
