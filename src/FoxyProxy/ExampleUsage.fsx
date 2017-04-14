#load "Reference.fsx"

open FoxyProxy

Firefox.createFirefoxProfile Firefox.profileDir
Firefox.setProxy Firefox.prefsFile
Firefox.startFirefox "http://www.bbc.com"

Proxy.StartProxy()

Proxy.BeforeRequest.Subscribe(printfn "%O")
it.Dispose()

let requResp = Proxy.GetRequest 76
requResp.RequestResponseData

let requResp2 = Proxy.GetRequest 4
requResp2.RequestResponseData

Proxy.GetAllRequests()
|> Explore.GroupRequests
|> Seq.filter(fun x ->  x.ContentTypes |> Seq.exists (fun x -> x = "text/html"))
//|> Seq.filter(fun x ->  x.UrlNoParametersNoExtenition.Contains("bbc"))
Replay.ReplayRequestResponseMeta true (Proxy.GetRequest 52)

let testRequ = Proxy.GetRequest 127
testRequ.RequestResponseData.Request
testRequ.RequestResponseData.Request.Body
