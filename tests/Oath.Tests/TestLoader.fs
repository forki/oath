namespace Oath.Test

module TestLoader =
    open Oath

    open System.Reflection
    open System.IO
    open System
    open System.Xml

    let transformerFromPath path =
        Path.Combine(Assembly.GetExecutingAssembly().Location, "../../../", path)
        |> Uri
        |> XmlDocument.FromUri
        |> Saxon.createTransformer
