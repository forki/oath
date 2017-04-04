namespace Oath

module XmlBuilder =
    open System.Xml

    let document str =
        let doc = new XmlDocument()
        doc.LoadXml(str)
        doc

    let element str =
        (document str).DocumentElement

    let attribute (qName: XmlQualifiedName) value =
        let attr = XmlDocument().CreateAttribute(qName.Name, qName.Namespace)
        attr.Value <- value
        attr

    let text str =
        let doc = XmlDocument()
        let t = doc.CreateTextNode(str)
        t
