namespace Oath

open System
open System.Xml
open System.Xml.Linq

[<AutoOpen>]
module Extensions =
    open System.IO
    open System.Xml.Schema

    type System.Collections.IEnumerator with
        member this.AsSeq =
            seq { while this.MoveNext() do yield this.Current }

    type String with
        member this.Xml =
            let fragment = XmlDocument().CreateDocumentFragment()
            fragment.InnerXml <- this
            fragment

    type XmlDocument with
        /// Load an [XmlDocument] from a [Uri].
        static member FromUri(uri: Uri) =
            let doc = XmlDocument()
            doc.Load(uri.AbsolutePath)
            doc

        static member LoadXml(str: string) =
            let doc = XmlDocument()
            doc.LoadXml(str)
            doc

        member this.XDocument =
            XDocument.Parse(this.OuterXml)

    type XmlAttribute with
        static member Create(name: string, value: string) =
            let attr = XmlDocument().CreateAttribute(name)
            attr.Value <- value
            attr

    type XmlSchema with
        static member FromString(str: string): XmlSchema =
            use stringReader = new StringReader(str)
            XmlSchema.Read(stringReader, (fun sender args -> ()))

    type XNode with
        /// Convert this [XNode] to an [XmlDocument].
        member this.XmlNode =
            XmlDocument().ReadNode(this.CreateReader())

type ResultType =
    | AtomicResult
    | NodeResult

type XmlValue<'n> =
    | Sequence of XmlValue<'n> seq
    | AtomicValue of obj
    | Node of XmlNode
    | PNode of 'n
    with
        member this.ResultType =
            match this with
            | AtomicValue _ -> AtomicResult
            | _ -> NodeResult

/// XSLT parameter scope.
type ParameterScope = Stylesheet | Template

/// An XSLT parameter declaration.
type Parameter<'n> = {
    values: (XmlQualifiedName * XmlValue<'n>) list;
    tunnel: bool;
    scope: ParameterScope
} with
    static member List(values: (XmlQualifiedName * XmlValue<'n>) list, ?scope, ?tunnel) =
        [{
            values = values
            tunnel = defaultArg tunnel false
            scope = defaultArg scope Template
        }]

type Arguments<'n> = XmlValue<'n> list

type TemplateApplication<'n, 'p> = {
    node: XmlValue<'n>
    mode: XmlQualifiedName option
    parameters: Parameter<'p> list
}

type TemplateCall<'n, 'p> = {
    name: XmlQualifiedName
    node: XmlValue<'n> option
    parameters: Parameter<'p> list
}

type FunctionCall<'p> = {
    name: XmlQualifiedName
    arguments: Arguments<'p>
    parameters: Parameter<'p> list
}

type Instruction<'n, 'p1, 'p2, 'p3> =
    | ApplyTemplate of TemplateApplication<'n, 'p1>
    | CallTemplate of TemplateCall<'n, 'p2>
    | CallFunction of FunctionCall<'p3>

type Template<'n> =
    static member Apply(node: XmlValue<'n>, ?mode, ?parameters) =
        ApplyTemplate {
            node = node
            mode = mode
            parameters = defaultArg parameters []
        }

    static member Call(name: XmlQualifiedName, ?node: XmlValue<'n>, ?parameters) =
        CallTemplate {
            name = name
            node = node
            parameters = defaultArg parameters []
        }

type Function =
    static member Call(name: XmlQualifiedName, arguments: Arguments<'n>, ?parameters) =
        CallFunction {
            name = name
            arguments = arguments
            parameters = defaultArg parameters []
        }

/// A function that transforms an XML [Value] to an XML [Value].
type XmlTransformer<'n> = XmlValue<'n> -> XmlValue<'n>

/// An XSLT transformer.
type XsltTransformer<'n, 'p1, 'p2, 'p3> = {
    /// Execute an XSLT instruction.
    Execute: XmlTransformer<'n> -> Instruction<'n, 'p1, 'p2, 'p3> -> ResultType -> XmlValue<'n>

    /// Take an XPath expression and a [Value<'n>] and return boolean that
    /// indicates whether the node matches the expression.
    Match: string -> XmlValue<'n> -> bool

    /// Select a node with XPath.
    Select: string -> XmlValue<'n> -> XmlValue<'n>

    /// Serialize a value into a string
    Serialize: XmlValue<'n> -> string

    Unwrap: 'n -> XmlNode
}

/// The configuration for an XSLT transformation.
type Configuration<'n, 'p1, 'p2, 'p3> = {
    transformer: XsltTransformer<'n, 'p1, 'p2, 'p3>
    inputRefiner: XmlTransformer<'n>
    controlRefiner: XmlTransformer<'n>
} with
    static member WithTransformer(transformer: XsltTransformer<'n, 'p1, 'p2, 'p3>) =
        {
            transformer = transformer
            inputRefiner = id
            controlRefiner = id
        }
