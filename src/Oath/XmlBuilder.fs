namespace Oath

module XmlBuilder =
    open System.Xml

    let document str =
        let doc = XmlDocument()
        doc.LoadXml(str)
        Node doc

    let element str =
        let doc = XmlDocument()
        doc.LoadXml(str)
        Node doc.DocumentElement

    let attribute (qname: XmlQualifiedName) value =
        let attr = XmlDocument().CreateAttribute(qname.Name, qname.Namespace)
        attr.Value <- value
        Node attr

    let text str =
        let doc = XmlDocument()
        let t = doc.CreateTextNode(str)
        Node t
