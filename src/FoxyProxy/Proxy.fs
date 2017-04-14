namespace FoxyProxy

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Net
open System.Text
open System.Threading.Tasks
open Titanium.Web.Proxy
open Titanium.Web.Proxy.EventArguments
open Titanium.Web.Proxy.Models

module Proxy =

    let private sync = new obj()
    let private requestsDict = new Dictionary<Guid,RequestResponseMeta>()
    let private requestsIndex = new List<Guid>()

    let GetRequest index =
        let criticalSection() =
            let guid = requestsIndex.[index]
            requestsDict.[guid]
        lock sync criticalSection 

    let GetAllRequests() =
        let criticalSection() =
            let copiedList = new ResizeArray<RequestResponseMeta>(requestsDict.Values)
            new ReadOnlyCollection<RequestResponseMeta>(copiedList)
        lock sync criticalSection

    let ClearRequests() =
        let criticalSection() =
            requestsDict.Clear()
            requestsIndex.Clear()
        lock sync criticalSection

    let private beforeRequestEvent = new Event<RequestResponseMeta>()
    let private beforeResponseEvent = new Event<RequestResponseMeta>()

    let BeforeRequest = beforeRequestEvent.Publish :> IObservable<RequestResponseMeta>
    let BeforeResponse = beforeResponseEvent.Publish :> IObservable<RequestResponseMeta>

    let private mapOfHeaders (headers: Dictionary<string,HttpHeader>) = 
        headers 
        |> Seq.map (fun kvp -> kvp.Value.Name, kvp.Value.Value)
        |> Map.ofSeq

    let private verbsWithBody = ["POST"; "PUT"]

    let private addRequest (ea: SessionEventArgs) =
        async { let requ = ea.WebSession.Request
                let! body = 
                    if Seq.contains (ea.WebSession.Request.Method.ToUpper()) verbsWithBody then
                        async { let! body = ea.GetRequestBody() |> Async.AwaitTask
                                return Some body }
                    else
                        async { return None }
                let critialSection() =
                    let headers = mapOfHeaders requ.RequestHeaders
                    let request = Request.Create requ.Method (requ.HttpVersion.ToString()) headers body
                    let requRespData = RequestResponseData.Create request
                    let requRespMeta = RequestResponseMeta.Create requestsIndex.Count requ.RequestUri requRespData
                    requestsDict.Add(ea.WebSession.RequestId, requRespMeta)
                    requestsIndex.Add(ea.WebSession.RequestId)
                    requRespMeta.Index
                return lock sync critialSection }

    let private addResponse (ea: SessionEventArgs) =
        async { let resp = ea.WebSession.Response
                let! body = ea.GetResponseBody() |> Async.AwaitTask
                let criticalSection() =
                    let requRespMeta = requestsDict.[ea.WebSession.RequestId] 
                    let headers = mapOfHeaders resp.ResponseHeaders 
                    let response = Response.Create resp.ResponseStatusCode "" headers body
                    let requRespData =
                        { requRespMeta.RequestResponseData with
                            Response  = Some response }
                    requestsDict.[ea.WebSession.RequestId] <-
                        { requRespMeta with
                            RequestResponseData = requRespData }
                    requRespMeta.Index
                return lock sync criticalSection }

    let private beforeRequestHandler (ea: SessionEventArgs) =
        async { let! index = addRequest ea
                do beforeRequestEvent.Trigger (GetRequest index) }
        |> Async.StartAsTask :> Task

    let private beforeResponseHandler (ea: SessionEventArgs) =
        async { let! index = addResponse ea
                do beforeResponseEvent.Trigger (GetRequest index) }
        |> Async.StartAsTask :> Task

    let private clientCertificateSelectionCallback (ea: CertificateSelectionEventArgs) =
        printfn "clientCertificateSelectionCallback: %s" (ea.TargetHost)
        Task.FromResult(()) :> Task

    let private serverCertificateValidationCallback (ea: CertificateValidationEventArgs) =
        printfn "ServerCertificateValidationCallback"
        Task.FromResult(()) :> Task

    let proxyPort = 8000

    let StartProxy() =
        let proxyServer = new ProxyServer()

        //locally trust root certificate used by this proxy 
        proxyServer.TrustRootCertificate <- true

        proxyServer.add_BeforeRequest(fun obj ea -> beforeRequestHandler ea)
        proxyServer.add_BeforeResponse(fun obj ea -> beforeResponseHandler  ea)

        let explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, proxyPort, true)

        proxyServer.ExceptionFunc <- new Action<Exception>(printfn "Exception: %O")
        proxyServer.AddEndPoint(explicitEndPoint)
        proxyServer.Start()



