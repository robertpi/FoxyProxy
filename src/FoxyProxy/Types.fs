namespace FoxyProxy

open System
open System.Text

type RequestHeaders = Map<string, string>
type ResponseHeaders = Map<string, string>
type RequestBody = option<byte[]>
type ResponseBody = byte[]

[<AutoOpen>]
module Printing =
    let mapToString (headers: Map<string, string>) =
        let headerSeq =
            seq { for kvp in headers do
                    yield sprintf "%s: %s" kvp.Key kvp.Value
                    yield Environment.NewLine }
        String.Concat(headerSeq)

    let requestHeadersToString (headers: RequestHeaders) =
        mapToString headers

    let requestBodyToTextString (body: RequestBody) =
        match body with
        | Some body ->
            // coz all text is UTF-8 right?
            (Encoding.UTF8.GetString(body))
        | None -> ""

    let responseHeadersToString (headers: ResponseHeaders) =
        mapToString headers

    let responseBodyToTextString (body: ResponseBody) =
        Encoding.UTF8.GetString(body)


type Request =
    { Verb: string
      HttpVersion: string
      Headers: RequestHeaders
      Body: RequestBody }
    with
        static member Create verb httpVer headers body =
            { Verb = verb
              HttpVersion = httpVer
              Headers = headers
              Body = body }
        override x.ToString() =
            seq { yield requestHeadersToString x.Headers
                  yield Environment.NewLine
                  yield requestBodyToTextString x.Body }
            |> fun respSeq -> String.Concat(respSeq)

type Response =
    { Status: string
      HttpVersion: string
      Headers: ResponseHeaders
      Body: ResponseBody }
    with
        static member Create status httpVer headers body =
            { Status = status
              HttpVersion = httpVer
              Headers = headers
              Body = body }
        override x.ToString() =
            seq { yield responseHeadersToString x.Headers
                  yield Environment.NewLine
                  yield responseBodyToTextString x.Body }
            |> fun respSeq -> String.Concat(respSeq)

type RequestResponseData =
    { Request: Request
      Response: option<Response> }
    with
        static member Create request =
            { Request = request 
              Response = None }
        override x.ToString() =
            seq { yield x.Request.ToString()
                  yield "-------------------"
                  yield Environment.NewLine
                  match x.Response with
                  | Some response -> yield response.ToString()
                  | None -> yield "response not available" }
            |> fun requRespSeq -> String.Concat(requRespSeq)

type RequestResponseMeta =
    { Index: int
      Url: Uri
      RequestResponseData: RequestResponseData }
    with
        static member Create index url requRespData =
            { Index = index
              Url = url
              RequestResponseData = requRespData }
        override x.ToString() =
            match x.RequestResponseData.Response with
            | Some response ->
                sprintf "[%i] %s %s" x.Index response.Status (x.Url.ToString())
            | None ->
                sprintf "[%i] %s %s" x.Index x.RequestResponseData.Request.Verb (x.Url.ToString())
