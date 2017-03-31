open System.Diagnostics
open System.IO

let firefoxLocation = @"C:\Program Files\Mozilla Firefox\firefox.exe"

let createFirefoxProfile profileDir =
    Process.Start(firefoxLocation, sprintf "-CreateProfile \"FoxyProxyProfile %s\"" profileDir)

let startFirefox url =
    Process.Start(firefoxLocation, sprintf "-no-remote -private-window -P FoxyProxyProfile %s" url)

let setProxy prefsFile =
    let settingPrefix = "network.proxy"
    let settings =
        [ "http", "\"localhost\""
          "http_port", "8000" 
          "no_proxies_on", "\"\"" 
          "ssl", "\"localhost\"" 
          "ssl_port", "8000" 
          "type", "1" ]

    let template =
        sprintf "user_pref(\"%s.%s\", %s);"

    let constructLine (setting, value) =
            template settingPrefix setting value

    let filteredFileLines = 
        File.ReadLines(prefsFile)
        |> Seq.filter(fun line -> 
            settings 
            |> Seq.exists (fun (setting, _) -> line.Contains(sprintf "\"%s.%s\"" settingPrefix setting))
            |> not)

    let newLines = Seq.map constructLine settings
    File.WriteAllLines(prefsFile, Seq.append filteredFileLines newLines |> Seq.toArray)

let profileDir = Path.Combine(__SOURCE_DIRECTORY__, "profile")
let prefsFile = Path.Combine(profileDir, "prefs.js")

createFirefoxProfile profileDir
setProxy prefsFile
startFirefox "http://localhost"

