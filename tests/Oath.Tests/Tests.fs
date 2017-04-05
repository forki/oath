namespace Oath.Tests

module OathTest =
    open Expecto
    open Oath
    open Oath.Saxon

    let config = fun () -> Configuration.WithTransformer (Saxon.getIdentityTransformer())

    [<Tests>]
    let tests = Expect.transformation config <| fun (==>) _ _ ->
        testList "Oath" [
            test "Can transform XML with XSLT" {
                document """<input/>""" |> Template.Apply ==> document """<input/>"""
            }
        ]

    [<EntryPoint>]
    let main args =
        runTestsInAssembly defaultConfig args
