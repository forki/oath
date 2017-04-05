namespace Oath.Tests

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
            test "Document node" {
                Expect.equal
                    (document "<x/>" |> getNodeKind)
                    (Some XmlNodeType.Document)
                    "documentNode"
            }

            test "Element" {
                Expect.equal
                    (element "<x/>" |> getNodeKind)
                    (Some XmlNodeType.Element)
                    "element"
            }

            test "Attribute" {
                Expect.equal
                    (attribute "foo" "bar" |> getNodeKind)
                    (Some XmlNodeType.Attribute)
                    "attribute"
            }

            test "Processing instruction" {
                Expect.equal
                    (pi "foo" "bar" |> getNodeKind)
                    (Some XmlNodeType.ProcessingInstruction)
                    "processing-instruction"
            }

            test "Text" {
                Expect.equal
                    (text "foo" |> getNodeKind)
                    (Some XmlNodeType.Text)
                    "text"
            }
        ]

