#r @"../../packages/Titanium.Web.Proxy/lib/net45/Titanium.Web.Proxy.dll"

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Net
open System.Text
open System.Threading.Tasks
open Titanium.Web.Proxy
open Titanium.Web.Proxy.EventArguments
open Titanium.Web.Proxy.Models

type RequestResponseData =
    { Index: int
      Url: Uri
      RequestVerb: string
      RequestHeaders: Dictionary<string, HttpHeader>
      RequestBody: byte[]
      ResponseHeaders: option<Dictionary<string, HttpHeader>>
      ResponseBody: option<byte[]> }
    with
        static member Create index url verb requestHeaders requestBody =
            { Index = index
              Url = url
              RequestVerb = verb
              RequestHeaders = requestHeaders
              RequestBody = requestBody
              ResponseHeaders = None
              ResponseBody = None }

let sync = new obj()
let requestsDict = new Dictionary<Guid,RequestResponseData>()
let requestsIndex = new List<Guid>()

let printRequest index (ea: SessionEventArgs) =
    printfn "[%i] %s %s" index ea.WebSession.Request.Method (ea.WebSession.Request.RequestUri.ToString())

let printResponse index (ea: SessionEventArgs) =
    printfn "[%i] %s %s" index ea.WebSession.Response.ResponseStatusCode (ea.WebSession.Request.RequestUri.ToString())

let verbsWithBody = ["POST"; "PUT"]
let addRequest (ea: SessionEventArgs) =
    async { let requ = ea.WebSession.Request
            let! body = 
                if Seq.contains (ea.WebSession.Request.Method.ToUpper()) verbsWithBody then
                    async { let! body = ea.GetRequestBody() |> Async.AwaitTask
                            return Some body }
                else
                    async { return None }
            return lock sync (fun () -> 
                            let requResp = RequestResponseData.Create requestsIndex.Count requ.RequestUri requ.Method requ.RequestHeaders body
                            requestsDict.Add(ea.WebSession.RequestId, requResp)
                            requestsIndex.Add(ea.WebSession.RequestId)
                            requResp.Index) }

let addResponse (ea: SessionEventArgs) =
    async { let resp = ea.WebSession.Response
            let! body = ea.GetResponseBody() |> Async.AwaitTask
            return lock sync (fun () ->
                         let requResp = requestsDict.[ea.WebSession.RequestId] 
                         requestsDict.[ea.WebSession.RequestId] <-
                            { requResp with
                                ResponseHeaders = Some resp.ResponseHeaders
                                ResponseBody = Some [||] }
                         requResp.Index) }

let beforeRequest (ea: SessionEventArgs) =
    async { let! index = addRequest ea
            do printRequest index ea }
    |> Async.StartAsTask :> Task

let beforeResponse (ea: SessionEventArgs) =
    async { let! index = addResponse ea
            do printResponse index ea }
    |> Async.StartAsTask :> Task

let clientCertificateSelectionCallback (ea: CertificateSelectionEventArgs) =
    printfn "clientCertificateSelectionCallback: %s" (ea.TargetHost)
    Task.FromResult(()) :> Task

let serverCertificateValidationCallback (ea: CertificateValidationEventArgs) =
    printfn "ServerCertificateValidationCallback"
    Task.FromResult(()) :> Task

let proxyPort = 8000

let startProxy() =
    let proxyServer = new ProxyServer();

    //locally trust root certificate used by this proxy 
    proxyServer.TrustRootCertificate <- true

    proxyServer.add_BeforeRequest(fun obj ea -> beforeRequest ea)
    proxyServer.add_BeforeResponse(fun obj ea -> beforeResponse  ea)

    let explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, proxyPort, true)

    proxyServer.ExceptionFunc <- new Action<Exception>(printfn "Exception: %O")
    proxyServer.AddEndPoint(explicitEndPoint)
    proxyServer.Start()

startProxy() 

let web = new WebClient()
web.Proxy <- new WebProxy("192.168.1.14", 8000)
let result = web.DownloadStringAsync(new Uri("http://www.bbc.com"))
web.DownloadStringCompleted.Add(fun x -> printfn "done")
