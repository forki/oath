namespace Oath

module Saxon =
    open Saxon.Api
    open System
    open System.Xml
    open System.Collections.Generic

    // TODO: Support Saxon configurations.
    let private processor = Processor(false)

    type XdmNode with
        // TODO: Add support for remaining relevant node types (comment, PI, etc.)
        member this.ToXmlNode() =
            let kind = this.NodeKind

            match kind with
            | XmlNodeType.Attribute ->
                let qname = this.NodeName.ToXmlQualifiedName()
                XmlBuilder.attribute qname (this.GetAttributeValue(this.NodeName))
            | XmlNodeType.Document -> XmlBuilder.document this.OuterXml
            | XmlNodeType.Element -> XmlBuilder.element this.OuterXml
            | XmlNodeType.Text -> XmlBuilder.text this.StringValue
            | _ ->
                failwithf "Cannot convert XdmNode of type %s into an XmlNode." (string kind)

    [<RequireQualifiedAccess>]
    module Builder =
        let documentBuilder = processor.NewDocumentBuilder()

        let build (node: XmlValue<XdmNode>) =
            match node with
            | Node n ->
                if (n.NodeType = XmlNodeType.Document) then
                    documentBuilder.Wrap(n :?> XmlDocument)
                else
                    let xdmNode = documentBuilder.Wrap(n.OwnerDocument)

                    xdmNode.EnumerateAxis(XdmAxis.DescendantOrSelf).AsSeq
                    |> Seq.cast<XdmNode>
                    |> Seq.find (fun node -> node.NodeKind = n.NodeType)
            | PNode n -> n
            | AtomicValue v ->
                failwithf "Can't convert atomic value %s into an XdmNode" (string v)

        let wrap (doc: XmlDocument) = PNode (documentBuilder.Wrap(doc))

    [<RequireQualifiedAccess>]
    module private XdmUtils =
        let toXdmAtomic value =
            match box value with
            | :? XdmAtomicValue as x -> x
            | :? string  as s   -> XdmAtomicValue(s)
            | :? int32   as i32 -> XdmAtomicValue(int64(i32))
            | :? int64   as i64 -> XdmAtomicValue(i64)
            | :? decimal as d   -> XdmAtomicValue(d)
            | :? float   as f   -> XdmAtomicValue(f)
            | :? bool    as b   -> XdmAtomicValue(b)
            | :? Uri     as u   -> XdmAtomicValue(u)
            | :? QName   as q   -> XdmAtomicValue(q)
            | _                 ->
                failwithf "Can't convert %s into an XdmAtomicValue." (string value)

        let toXdmValue value =
            match value with
            | AtomicValue v -> v |> toXdmAtomic :> XdmValue
            | Node _ -> value |> Builder.build :> XdmValue
            | PNode n -> n :> XdmValue

        let dictionarize (xs: (XmlQualifiedName * XmlValue<XdmNode>) list) =
            xs
            |> List.map (fun (name: XmlQualifiedName, value) -> (QName name), value |> toXdmValue)
            |> dict
            |> Dictionary

        let toObj (value: XdmValue) =
            match box value with
            | :? XdmNode as node -> node.getUnderlyingXmlNode() :> obj
            | :? XdmAtomicValue as value -> value.Value
            | _ -> failwithf "Can't convert %s into a native .NET value." (string value)

    [<RequireQualifiedAccess>]
    module XSLT =
        [<Literal>]
        let IdentityTransform =
            """
            <xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="3.0">
                <xsl:template match="@* | node()">
                    <xsl:copy>
                        <xsl:apply-templates select="@* | node()"/>
                    </xsl:copy>
                </xsl:template>
            </xsl:stylesheet>
            """

        let compiler = processor.NewXsltCompiler()

        let compileUri (stylesheet: Uri) = compiler.Compile(stylesheet)

        let compileXmlNode (node: XmlNode) =
            node |> Builder.documentBuilder.Build |> compiler.Compile

        let setContextNode (node: XmlValue<XdmNode> option) (transformer: Xslt30Transformer) =
            node |> Option.iter (fun n ->
                transformer.GlobalContextItem <- n |> Builder.build)

            transformer

        let setParameters parameters (transformer: Xslt30Transformer) =
            parameters |> List.iter (fun parameters ->
                let dictionary = parameters.values |> XdmUtils.dictionarize

                match parameters.scope with
                | Template -> transformer.SetInitialTemplateParameters(dictionary, parameters.tunnel)
                | Stylesheet -> transformer.SetStylesheetParameters(dictionary))

            transformer

        let setMode (mode: QName option) (transformer: Xslt30Transformer): Xslt30Transformer =
            mode |> Option.iter (fun m -> transformer.InitialMode <- m)
            transformer

        let getTransformer (executable: XsltExecutable) parameters =
            let transformer = executable.Load30()
            transformer.GetUnderlyingController.setRecoveryPolicy(2)
            transformer |> setParameters parameters

        let transform resultType (transformation: unit -> XdmValue) =
            match resultType with
            | AtomicResult -> AtomicValue (transformation() |> XdmUtils.toObj)
            | NodeResult ->
                (transformation() :?> XdmNode).ToXmlNode()

        let applyTemplates executable resultType node mode parameters =
            let transformer = getTransformer executable parameters |> setMode mode

            transform resultType (fun () -> transformer.ApplyTemplates(node |> Builder.build))

        let callTemplate executable resultType name node parameters =
            let transformer = getTransformer executable parameters |> setContextNode node

            transform resultType (fun () -> transformer.CallTemplate(name))

        let callFunction executable resultType name args parameters =
            let arguments = args |> List.map XdmUtils.toXdmValue |> Array.ofList
            let transformer = getTransformer executable parameters

            transform resultType (fun () -> transformer.CallFunction(name, arguments))

    [<RequireQualifiedAccess>]
    module XPath =
        let compiler =
            let c = processor.NewXPathCompiler()
            c.DeclareNamespace("xs", "http://www.w3.org/2001/XMLSchema")
            c

        let select query node = compiler.Evaluate(query, node)

        let selectNode query (value: XmlValue<XdmNode>) =
            PNode (select query (Builder.build value) :?> XdmNode)

        let matches (query: string) (ctx: XmlValue<XdmNode>): bool =
            let selector = compiler.Compile(query).Load()
            selector.ContextItem <- (Builder.build ctx :> XdmItem)
            selector.EffectiveBooleanValue()

    let attribute name value =
        let doc = XmlDocument()
        let el = doc.CreateElement("x")
        let attr = doc.CreateAttribute(name)
        attr.Value <- value
        el.SetAttributeNode(attr) |> ignore
        doc.AppendChild(el) |> ignore

        XPath.selectNode (sprintf "/x/@%s" name) (Builder.wrap doc)

    let document (str: string) =
        let doc = XmlDocument()
        doc.LoadXml(str)
        Builder.wrap doc

    let element str = XPath.selectNode "/*" (document str)

    let pi target data =
        let doc = XmlDocument()
        let pi = doc.CreateProcessingInstruction(target, data)
        doc.AppendChild(pi) |> ignore

        XPath.selectNode (sprintf "/processing-instruction('%s')" target) (Builder.wrap doc)

    let text str =
        let doc = XmlDocument()
        let el = doc.CreateElement("x")
        let t = doc.CreateTextNode(str)
        el.AppendChild(t) |> ignore
        doc.AppendChild(el) |> ignore

        XPath.selectNode "/x/text()" (Builder.wrap doc)

    let createTransformer (stylesheet: XmlNode) =
        let executable = XSLT.compileXmlNode stylesheet

        {
            Execute = fun (refiner: XmlTransformer<XdmNode>) instruction resultType ->
                match instruction with
                | ApplyTemplate { node = node; mode = mode; parameters = parameters } ->
                    let mode' = Option.map (fun (m: XmlQualifiedName) -> QName(m)) mode
                    XSLT.applyTemplates executable resultType (refiner node) mode' parameters
                | CallTemplate { name = name; node = node; parameters = parameters } ->
                    XSLT.callTemplate executable resultType (QName name) (Option.map refiner node) parameters
                | CallFunction { name = name; arguments = arguments; parameters = parameters } ->
                    XSLT.callFunction executable resultType (QName name) arguments parameters

            Match = XPath.matches

            Select = XPath.selectNode

            Serialize = fun value ->
                match value with
                | AtomicValue v -> v.ToString()
                | Node n -> n.OuterXml
                | PNode n -> n.OuterXml

            Unwrap = fun node -> node.getUnderlyingXmlNode()
        }

    let getIdentityTransformer() = createTransformer XSLT.IdentityTransform.Xml
