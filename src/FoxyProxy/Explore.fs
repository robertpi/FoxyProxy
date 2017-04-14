namespace FoxyProxy

open System
open System.IO

type RequestStat =
    { Url: Uri
      UrlNoParametersNoExtenition: string
      UrlExtenition: option<string>
      ContentType: option<string> }
      with
        static member CreateOrNone (requResp: RequestResponseMeta) =
            match requResp.RequestResponseData.Response with
            | Some resp ->
                let noParameters = requResp.Url.GetLeftPart(UriPartial.Path)
                let ext = Path.GetExtension(noParameters)
                let extOpt =
                    if String.IsNullOrWhiteSpace(ext) then
                        None
                    else
                        Some ext
                { Url = requResp.Url
                  UrlNoParametersNoExtenition = noParameters
                  UrlExtenition = extOpt
                  ContentType = resp.Headers.TryFind "Content-Type" }
                |> Some
            | None -> None

type RequestSummaryLine =
    { UrlNoParametersNoExtenition: string
      Count: int
      UrlExtenitions: seq<string>
      ContentTypes: seq<string> }
      with
        static member Create baseUrl (requResp: seq<RequestStat>) =
            let distinctList proj =
                requResp |> Seq.choose proj |> Seq.distinct |> Seq.toList
            { UrlNoParametersNoExtenition = baseUrl
              Count = Seq.length requResp
              UrlExtenitions =  distinctList (fun x -> x.UrlExtenition)
              ContentTypes = distinctList (fun x -> x.ContentType) }

module Explore =
    
    let GroupRequests (requs: seq<RequestResponseMeta>) =
        let requestStats = 
            requs
            |> Seq.choose RequestStat.CreateOrNone
        requestStats
        |> Seq.groupBy (fun x -> x.UrlNoParametersNoExtenition)
        |> Seq.map (fun (urlBase, coll) ->  RequestSummaryLine.Create urlBase coll)

