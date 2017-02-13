namespace Oath.Test

module OathTest =
    open Expecto
    open Oath

    let config = fun () -> Configuration.WithTransformer (Saxon.getIdentityTransformer())

    [<Tests>]
    let tests = Expect.transformation config <| fun (==>) _ _ ->
        testList "Oath" [
            testCase "Can transform XML with XSLT" <| fun _ ->
                XML """<input/>""" |> Template.Apply ==> XML """<input/>"""
        ]

    [<EntryPoint>]
    let main args =
        runTestsInAssembly defaultConfig args
