namespace Oath.Test

open Expecto

open Oath
open Oath.Saxon

open Saxon.Api
open System.Xml

module SaxonTest =
    let getNodeKind node =
        match node with
        | PNode (n: XdmNode) -> Some n.NodeKind
        | _ -> None

    [<Tests>]
    let tests =
        testList "Saxon XML node types" [
            testCase "Document node" <| fun _ ->
                Expect.equal
                    (documentNode "<x/>" |> getNodeKind)
                    (Some XmlNodeType.Document)
                    "documentNode"

            testCase "Element" <| fun _ ->
                Expect.equal
                    (element "<x/>" |> getNodeKind)
                    (Some XmlNodeType.Element)
                    "element"

            testCase "Attribute" <| fun _ ->
                Expect.equal
                    (attribute "foo" "bar" |> getNodeKind)
                    (Some XmlNodeType.Attribute)
                    "attribute"

            testCase "Processing instruction" <| fun _ ->
                Expect.equal
                    (pi "foo" "bar" |> getNodeKind)
                    (Some XmlNodeType.ProcessingInstruction)
                    "processing-instruction"

            testCase "Text" <| fun _ ->
                Expect.equal
                    (text "foo" |> getNodeKind)
                    (Some XmlNodeType.Text)
                    "text"
        ]

