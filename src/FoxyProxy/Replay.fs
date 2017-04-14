namespace FoxyProxy

open System
open System.IO
open System.Net

module Replay =
    let ReplayRequest useProxy (url: Uri) (requ: Request) =
        let web = WebRequest.Create(url) :?> HttpWebRequest

        if useProxy then
            web.Proxy <- new WebProxy("localhost", 8000)

        web.Method <- requ.Verb
        let headersTable = 
            [ "Accept", fun x -> web.Accept <- x; 
              "Connection", fun x -> ()// web.Connection <- x; // TODO can cause errors
              "Content-Length", fun x -> ();
              "Date", fun x -> () //web.Date <- x; // TODO parse date time
              "Expect", fun x -> web.Expect <- x;
              "Host", fun x -> web.Host <- x;
              "If-Modified-Since", fun x -> () //web.IfModifiedSince <- x; // TODO parse date time
              "Proxy-Connection", fun x -> ();
              "Range", fun x -> ();
              "Referer", fun x -> web.Referer <- x;
              "User-Agent", fun x -> web.UserAgent <- x; ]
        let ignoreHeaders = Seq.map fst headersTable
        for header in requ.Headers |> Seq.filter (fun x -> Seq.contains x.Key ignoreHeaders |> not) do
            web.Headers.[header.Key] <- header.Value

        let setIfPresent key setter =
            match Map.tryFind key requ.Headers with
            | Some value -> setter value
            | None -> ()
        
        for (key, setter) in headersTable do
            setIfPresent key setter
    

        match requ.Body with
        | Some body ->
            use requStream = web.GetRequestStream()
            requStream.Write(body, 0, body.Length)
        | None -> ()

        // 304 request throw an error here
        use response = web.GetResponse()
        use dataStream = response.GetResponseStream()
        use reader = new StreamReader (dataStream);
        let responseFromServer = reader.ReadToEnd ()
        ()

    let ReplayRequestResponseMeta useProxy (requResp: RequestResponseMeta) =
        ReplayRequest useProxy requResp.Url requResp.RequestResponseData.Request


