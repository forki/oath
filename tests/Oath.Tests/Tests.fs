namespace Oath.Tests

module OathTest =
    open Expecto
    open Oath
    open Oath.Saxon

    let config = fun () -> Configuration.WithTransformer (Saxon.getIdentityTransformer())

    [<Tests>]
    let tests = Expect.transformation config <| fun (==>) _ _ ->
        testList "Oath" [
            testCase "Can transform XML with XSLT" <| fun _ ->
                document """<input/>""" |> Template.Apply ==> document """<input/>"""
        ]

    [<EntryPoint>]
    let main args =
        runTestsInAssembly defaultConfig args
