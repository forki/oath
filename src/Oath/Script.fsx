#r @"..\..\packages\Saxon-HE\lib\net40\saxon9he-api.dll"

open Saxon.Api
open System
open System.Xml

let processor = Processor(false)
let builder = processor.NewDocumentBuilder()
