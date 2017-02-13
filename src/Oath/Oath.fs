namespace Oath

open System.Xml

module Oath =
    open Org.XmlUnit.Builder

    /// Compute the [Diff] of two [XmlNode] instances.
    let computeDiff (actual: XmlNode) (control: XmlNode) =
        DiffBuilder.Compare(Input.FromNode(control)).WithTest(Input.FromNode(actual)).Build()

    /// Given a [Configuration] and a [ResultType], execute the given [Instruction] and return the
    /// resulting [Value].
    ///
    /// If the requested [ResultType] is [AtomicResult], the transformation returns an
    /// [AtomicValue]. If it's [NodeResult], the transformation returns a [Node].
    let execute config resultType instruction =
        config.transformer.Execute config.inputRefiner instruction resultType

    /// Refine an [Instruction] by select a node from the input node with XPath.
    ///
    /// Useful when your XSLT transformation is context-dependent.
    ///
    /// Example:
    ///
    /// ```
    /// ApplyTemplate { node = """<parent number="42"><child dependsOn="parent"/></parent>"""; mode = None; parameters = [] }
    /// </> config "parent/child"
    /// <?> "output[number(@number) eq 42]"
    /// ```
    ///
    /// The template for `<child/>` retrieves information from `<parent/>`, but in this test we're
    /// only interested `<child/>`, not `<parent/>`. After all, `<parent/>` could potentially have
    /// a million child nodes.
    let select config instruction expression =
        let sel = config.transformer.Select expression

        match instruction with
        | ApplyTemplate a -> ApplyTemplate { a with node = sel a.node }
        | CallTemplate c -> CallTemplate { c with node = Option.map sel c.node }
        | CallFunction _ -> instruction

[<AutoOpen>]
module AutoOpen =
    /// Construct an XML node from a string. You can use the resulting [Node] to create an
    /// [Instruction].
    let XML (str: string) = Node str.Xml

    /// Create an [XmlQualifiedName] instance.
    let Q = XmlQualifiedName

    /// Create a namespaced [XmlQualifiedName] instance.
    let Q2 ns name = XmlQualifiedName(name, ns)
