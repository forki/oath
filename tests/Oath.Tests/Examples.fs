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
            testCase "Apply a template with no parameters in the default mode" <| fun () ->
                /// `Template.apply` the most concise way of expressing a XSLT template application.
                /// It creates an <xsl:apply-templates/> instruction.
                ///
                /// `==>` executes the instruction and compares the result of the transformation
                /// against the control XML node.
                XML """<input/>""" |> Template.Apply ==> XML """<output/>"""

            testCase "Apply a template with a parameter in the default mode" <| fun () ->
                /// A more explicit way of expressing an XSLT template application is to use the
                /// `ApplyTemplate` type.
                ///
                /// With it, you need to specify the mode and the parameters explicitly.
                ApplyTemplate {
                    node = XML """<input number="1"/>"""
                    mode = None
                    /// `Q` is shorthand for creating an `XmlQualifiedName`.
                    parameters = Parameter.List [(Q "number", 42L)]
                } ==> XML """<output number="42"/>"""

            testCase "Apply a template in a non-default mode" <| fun () ->
                ApplyTemplate {
                    node = XML """<input/>"""
                    mode = Some (Q "non-default")
                    parameters = []
                } ==> XML """<output mode="non-default"/>"""

            testCase "Call a template" <| fun () ->
                CallTemplate {
                    name = Q "named-template"
                    parameters = Parameter.List [(Q "number", 84L)]
                    node = None
                } ==> XML """<output number="84"/>"""

            testCase "Call a template and set a context node" <| fun () ->
                /// If your template or function takes some other XML node type than document node,
                /// you have to create that type of node.
                ///
                /// For Saxon, the `Oath.Saxon` module contains functions for creating different
                /// XML node types.
                Template.Call (Q "named-template", element """<input number="1"/>""")
                ==> XML """<output number="10"/>"""

            testCase "Select a context node, apply the template for that node, and check whether the result matches an XPath expression" <| fun () ->
                /// Define an input XML fragment.
                XML """
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

            testCase "Call an XSLT function" <| fun () ->
                /// Alternatively, use `CallFunction`.
                Function.Call (Q2 "local" "reverse", ["foo"]) ==> AtomicValue "oof"
        ]
