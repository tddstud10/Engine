module R4nd0mApps.TddStud10.Common.JsonConvert

open Newtonsoft.Json

let deserialize<'a> s = JsonConvert.DeserializeObject<'a>(s)
let serialize o = JsonConvert.SerializeObject(o)
