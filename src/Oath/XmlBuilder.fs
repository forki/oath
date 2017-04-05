namespace Oath

module internal XmlBuilder =
    open System.Xml

    let document str =
        XmlDocument.LoadXml(str) :> XmlNode |> Node

    let element str =
        XmlDocument.LoadXml(str).DocumentElement :> XmlNode |> Node

    let attribute (qname: XmlQualifiedName) value =
        let attr = XmlDocument().CreateAttribute(qname.Name, qname.Namespace)
        attr.Value <- value
        Node attr

    let text str =
        let t = XmlDocument().CreateTextNode(str)
        Node t
