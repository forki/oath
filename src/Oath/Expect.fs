namespace Oath

module Expect =
    open Expecto

    open Org.XmlUnit.Builder
    open Org.XmlUnit.Validation

    open System
    open System.Xml
    open System.Xml.Schema

    /// Check whether two [XmlNode] instances are equal.
    let xmlEquals (actual: XmlNode) (control: XmlNode) =
        let diff = Oath.computeDiff actual control
        Expect.isFalse (diff.HasDifferences()) (diff.ToString())

    /// Execute an [Instruction] and check whether the result matches an XPath
    /// expression.
    ///
    /// Example:
    ///
    /// ```
    /// let instruction = ApplyTemplate {
    ///     node = XML """<foo bar="baz"/>"""
    ///     mode = None
    ///     parameters = []
    /// }
    ///
    /// Expect.matches config instruction "quux[@cargle eq 'quuz']"
    /// ```
    let matches config instruction expression =
        let actual = Oath.execute config ResultType.NodeResult instruction

        match actual with
        | AtomicValue _ ->
            Tests.failtestf """The result of the transformation is not an XML node. It won't match any XPath expression."""
        | _ ->
            if (config.transformer.Match expression actual |> not) then
                Tests.failtestf """Expected transformation result to match XPath expression %s but result was: %s"""
                    expression (config.transformer.Serialize actual)

    /// Execute an [Instruction] and check whether the result equals the control
    /// XML [Node].
    let yields config instruction (control: Value<'n>) =
        let result = Oath.execute config control.ResultType instruction
        let unwrap = config.transformer.Unwrap

        match (result, control |> config.controlRefiner) with
        | AtomicValue a, AtomicValue c ->
            Expect.equal a c "The transformation yields the expected atomic value."
        | Node a,  Node c  -> xmlEquals a c
        | PNode a, Node c  -> xmlEquals (unwrap a) c
        | Node a,  PNode c -> xmlEquals a (unwrap c)
        | PNode a, PNode c -> xmlEquals (unwrap a) (unwrap c)
        | _ ->
            Tests.failtestf """Mismatch between actual value of type %s and control value of type %s: can only compare atomic values or nodes."""
                (result.GetType().ToString()) (control.GetType().ToString())

    let private validate (schema: XmlSchema) (node: XmlNode) =
        let v = Validator.ForLanguage(Languages.W3C_XML_SCHEMA_NS_URI)
        v.Schema <- schema
        v.ValidateInstance(Input.FromNode(node).Build())

    /// Execute an [Instruction] and check whether the result validates against
    /// the given [XmlSchema].
    let validatesAgainst config instruction (schema: XmlSchema) =
        let result = Oath.execute config ResultType.NodeResult instruction

        match result with
        | AtomicValue _ ->
            Tests.failtest "XML schema validation failed: the transformation yields an atomic value."
        | Node n ->
            let result = validate schema n
            Expect.isTrue result.Valid (string result.Problems)
        | PNode _ -> raise (NotImplementedException())

    /// Given a [Configuration] and a function, execute the function with
    /// functions curried with the given [Configuration] as the arguments.
    ///
    /// Example:
    /// ```
    /// Expect.transformation config <| fun (==>) (-->) (<*>) (/*) ->
    ///     testCase "<input> becomes <output>" <| fun _ ->
    ///         ApplyTemplate { At with node = Xml """<input/>""" } ==> Xml """<output number="42"/>"""
    /// ```
    let transformation (initializer: unit -> Configuration<'n, 'p1, 'p2, 'p3>) transform: Test =
        let config = initializer()
        transform (yields config) (matches config) (Oath.select config)
