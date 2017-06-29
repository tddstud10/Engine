[<EntryPoint>]
let main (argv : string[]) =
    if argv.Length <> 0 then
        R4nd0mApps.TddStud10.Engine.ServerV1.main argv
    else
        R4nd0mApps.TddStud10.Engine.Serverv2.main argv
