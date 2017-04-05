namespace Oath.Tests

module Examples =
    open Expecto

    open Oath
    open Oath.Saxon

    let config = fun () ->
        Configuration.WithTransformer (TestLoader.transformerFromPath "Examples.xsl")

    [<Tests>]
    let tests = Expect.transformation config <| fun (==>) (<?>) (</>) ->
        testList "Test XSLT with F#" [
            test "Apply a template with no parameters in the default mode" {
                /// `Template.apply` the most concise way of expressing a XSLT template application.
                /// It creates an <xsl:apply-templates/> instruction.
                ///
                /// `==>` executes the instruction and compares the result of the transformation
                /// against the control XML node.
                document """<input/>""" |> Template.Apply ==> document """<output/>"""
            }

            test "Apply a template with a parameter in the default mode" {
                /// A more explicit way of expressing an XSLT template application is to use the
                /// `ApplyTemplate` type.
                ///
                /// With it, you need to specify the mode and the parameters explicitly.
                ApplyTemplate {
                    node = document """<input number="1"/>"""
                    mode = None
                    /// `Q` is shorthand for creating an `XmlQualifiedName`.
                    parameters = Parameter.List [(Q "number", AtomicValue 42L)]
                } ==> document """<output number="42"/>"""
            }

            test "Apply a template in a non-default mode" {
                ApplyTemplate {
                    node = document """<input/>"""
                    mode = Some (Q "non-default")
                    parameters = []
                } ==> document """<output mode="non-default"/>"""
            }

            test "Call a template" {
                CallTemplate {
                    name = Q "named-template"
                    parameters = Parameter.List [(Q "number", AtomicValue 84L)]
                    node = None
                } ==> document """<output number="84"/>"""
            }

            test "Call a template and set a context node" {
                Template.Call (Q "named-template", element """<input number="1"/>""")
                ==> document """<output number="10"/>"""
            }

            test "Select a context node, apply the template for that node, and check whether the result matches an XPath expression" {
                /// Define an input XML fragment.
                document """
                <parent number="42">
                    <child dependsOn="parent"/>
                </parent>
                """
                /// Create a an `ApplyTemplate` instruction for that fragment.
                |> Template.Apply
                /// Select the `<child>` element.
                </> "parent/child"
                /// Execute the `ApplyTemplate` instruction on the `<child>` element and check
                /// whether it matches the given XPath expression.
                <?> "output[xs:integer(@number) eq 42]"
            }

            test "Call an XSLT function" {
                Function.Call (Q2 "local" "reverse", [AtomicValue "foo"]) ==> AtomicValue "oof"
            }

            test "Call an XSLT function that returns multiple values" {
                Function.Call (Q2 "local" "double", [AtomicValue 1; AtomicValue 2])
                ==> Sequence [AtomicValue 2; AtomicValue 4]
            }

            test "Call an XSLT function that returns an element" {
                Function.Call (Q2 "local" "wrap", [element "<foo/>"]) ==> element "<bar><foo/></bar>"
            }
        ]
