#r @"bin\Debug\FoxyProxy.dll"
open System
open FoxyProxy

let line = new String('-', 96)

let chop (target: string) chars =
    let charWithoutEllipis = chars - 4 
    if target.Length > chars - 4 then
        target.[ .. charWithoutEllipis - 1] + " ..."
    else
        target


let printRequestSummaryLines (summaryLines: seq<RequestSummaryLine>) =
        
    let h1 = "Base url"
    let h2 = "Count"
    let h3 = "Ext."
    let h4 = "Content Types"

    let printRow = sprintf "|%-40s| %8s|%-8s|%-34s|"

    let lines = 
        seq { yield ""
              yield line
              yield printRow h1 h2 h3 h4 
              yield line
              for x in summaryLines do
                  yield printRow
                      (chop x.UrlNoParametersNoExtenition 40)
                      (x.Count.ToString())
                      (String.Join(", ", x.UrlExtenitions))
                      (chop (String.Join(", ", x.ContentTypes)) 34)
              yield line }
    String.Join(Environment.NewLine, lines)

let registerPrinters() =
    fsi.AddPrinter(requestHeadersToString)
    fsi.AddPrinter(requestBodyToTextString)
    fsi.AddPrinter(fun (x:Request) -> x.ToString())
    fsi.AddPrinter(responseHeadersToString)
    fsi.AddPrinter(responseBodyToTextString)
    fsi.AddPrinter(fun (x:Response) -> x.ToString())
    fsi.AddPrinter(fun (x:RequestResponseData) -> x.ToString())
    fsi.AddPrinter(fun (x:RequestResponseMeta) -> x.ToString())
    fsi.AddPrinter(printRequestSummaryLines)

registerPrinters()
