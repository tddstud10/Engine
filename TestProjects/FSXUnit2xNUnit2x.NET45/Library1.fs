namespace FSXUnit2xNUnit2x

#if BREAK_BUILD
#else
open global.Xunit
open FsCheck.Xunit
open FsCheck
open FsUnit
#endif

type Class1() = 
    [<Property>]
    let ``Reverse of reverse of a list is the original list``(xs:list<int>) =
        List.rev(List.rev xs) = 
            xs @ 
#if BREAK_TEST
            [0xdeadbeef]
#else
            []
#endif

    let run c (s : string) = s.Split([|c|]).[0]

    [<Theory>]
#if BREAK_TEST
    [<InlineData("an1 rest",           "?an1")>]
#endif
    [<InlineData("an1 rest",           "an1")>]
    [<InlineData("?test bla",          "?test")>]
    let ``should parse symbol``(toParse:string, result:string) =
        Assert.Equal(result, run ' ' toParse)

//    [<Property>]
//    let ``Reverse of reverse of a list is the original list ``(xs:list<int>) =
//        List.rev(List.rev xs) = xs

    [<NUnit.Framework.Test>]
    member __.shouldSayHelloWithFsUnit () = 
        ("Hello " + 
#if BREAK_TEST
            "???"
#else
            "World!"
#endif
        ) 
        |> should equal "Hello World!"
