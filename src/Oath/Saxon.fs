namespace Oath

module Saxon =
    open Saxon.Api
    open System
    open System.Xml
    open System.Collections.Generic

    // TODO: Support Saxon configurations.
    let private processor = Processor(false)

    [<RequireQualifiedAccess>]
    module Builder =
        let documentBuilder = processor.NewDocumentBuilder()

        let toXdmNode (node: Value<XdmNode>) =
            match node with
            | Node n -> documentBuilder.Build(n)
            | PNode n -> n
            | AtomicValue v ->
                failwithf "Can't convert atomic value %s into an XdmNode" (v.ToString())

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
            | _                 -> failwithf "Can't convert %s into an XdmAtomicValue." (value.ToString())

        let toXdmValue value =
            match box value with
            | :? XdmNode as n -> n :> XdmValue
            | _ -> (value |> toXdmAtomic) :> XdmValue

        let dictionarize xs =
            xs
            |> List.map (fun (name: XmlQualifiedName, value) -> (QName name), value |> toXdmValue)
            |> dict |> Dictionary

        let toObj (value: XdmValue) =
            match box value with
            | :? XdmNode as node -> node.getUnderlyingXmlNode() :> obj
            | :? XdmAtomicValue as value -> value.Value
            | _ -> value.ToString() |> sprintf "Can't convert %s into a native .NET value." |> failwith

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

        let setContextNode (node: Value<XdmNode> option) (transformer: Xslt30Transformer) =
            node |> Option.iter (fun n ->
                transformer.GlobalContextItem <- n |> Builder.toXdmNode)

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

        let transform resultType atom (node: XmlDestination -> unit) =
            match resultType with
            | AtomicResult -> AtomicValue (atom() |> XdmUtils.toObj)
            | NodeResult ->
                let destination = XdmDestination()
                node destination
                Node (destination.XdmNode.getUnderlyingXmlNode())

        let applyTemplates executable resultType node mode parameters =
            let transformer = getTransformer executable parameters |> setMode mode

            transform resultType
                (fun () -> transformer.ApplyTemplates(node |> Builder.toXdmNode))
                (fun destination -> transformer.ApplyTemplates(node |> Builder.toXdmNode, destination))

        let callTemplate executable resultType name node parameters =
            let transformer = getTransformer executable parameters |> setContextNode node

            transform resultType
                (fun () -> transformer.CallTemplate(name))
                (fun destination -> transformer.CallTemplate(name, destination))

        let callFunction executable resultType name args parameters =
            let arguments = args |> List.map XdmUtils.toXdmValue |> Array.ofList
            let transformer = getTransformer executable parameters

            transform resultType
                (fun () -> transformer.CallFunction(name, arguments))
                (fun destination -> transformer.CallFunction(name, arguments, destination))

    [<RequireQualifiedAccess>]
    module XPath =
        let compiler =
            let c = processor.NewXPathCompiler()
            c.DeclareNamespace("xs", "http://www.w3.org/2001/XMLSchema")
            c

        let select query node = compiler.Evaluate(query, node)

        let selectNode query (value: Value<XdmNode>) =
            PNode (select query (Builder.toXdmNode value) :?> XdmNode)

        let matches (query: string) (ctx: Value<XdmNode>): bool =
            let selector = compiler.Compile(query).Load()
            selector.ContextItem <- (Builder.toXdmNode ctx :> XdmItem)
            selector.EffectiveBooleanValue()

    let attribute name value =
        let doc = XmlDocument()
        let el = doc.CreateElement("x")
        let attr = doc.CreateAttribute(name)
        attr.Value <- value
        el.SetAttributeNode(attr) |> ignore
        doc.AppendChild(el) |> ignore

        XPath.selectNode (sprintf "/x/@%s" name) (Builder.wrap doc)

    let documentNode (str: string) = Builder.wrap str.Xml

    let element str = XPath.selectNode "/*" (documentNode str)

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
        }

    let getIdentityTransformer() = createTransformer XSLT.IdentityTransform.Xml
