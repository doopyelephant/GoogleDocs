using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDocs;

public static class CookieManager
{
    public static MainWindow mainWindow;
    private static string cookiescript = "../../../browser_cookies.py";
    private static string authcookie = "";
    private static bool hasinit = false;
    private static SaveKeys SaveKeys;
    private static UrlConfig UrlConfig = new UrlConfig();
    private static string browsercookiepath = "";
    private static bool CookieSelectorCallback = false;
    private static bool alphabetical = true;
    private static string ExecuteScript(string fileName, params string[] arguments)
    {
        Process process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (!string.IsNullOrWhiteSpace(error))
        {
            output += (output.Length > 0 ? Environment.NewLine : string.Empty) + error;
        }

        return output;
    }
    public static void OvverideAlphabetical(bool val)
    {
        alphabetical = val;
    }

    private static BrowserCookieJar GetCookies(string hostfilter = "")
    {
        string datadir = "";
        if(OperatingSystem.IsWindows())
        {
         datadir = "C:\\Users\\nolan\\AppData\\Roaming\\GoogleDocs";
       // string profiledir = "C:\\Users\\nolan\\AppData\\Roaming\\zen\\Profiles\\us8cxx3x.Default (alpha)";
        string profiledir = "C:\\Users\\nolan\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\emrp3qaz.default-release";
        }
        else{
            datadir = "~\\.config\\GoogleDocs";
        }
        Console.WriteLine(ExecuteScript("mkdir",datadir));
        /*//string cookiepath =
         //   "C:\\Users\\nolan\\AppData\\Roaming\\zen\\Profiles\\us8cxx3x.Default (alpha)\\cookies.sqlite";
         string cookiepath = GetBrowserCookiePath().Result;
        string cpcmd = cookiepath.AddQuotes() + " " + (datadir + "\\cookies.sqlite").AddQuotes();
        
        string file = "copy";
        if(OperatingSystem.IsWindows())
        {
            file = "copy";
        }
        else if(OperatingSystem.IsLinux())
        {
            file = "cp";
        }
        else
        {

        }
        Console.WriteLine(cpcmd);
        Console.WriteLine(ExecuteScript(cpcmd));
        cpcmd = "copy " + cookiepath.AddQuotes() + " " + (datadir + "\\key4.db").AddQuotes();
        cookiepath =
            "C:\\Users\\nolan\\AppData\\Roaming\\zen\\Profiles\\us8cxx3x.Default (alpha)\\key4.db";
 Console.WriteLine(cpcmd);
        Console.WriteLine(ExecuteScript(cpcmd));*/
// Copy all three WAL files
ExecuteScript("copy " + Path.Combine(profiledir, "cookies.sqlite").AddQuotes() + " " + Path.Combine(datadir, "cookies.sqlite").AddQuotes());
ExecuteScript("copy " + Path.Combine(profiledir, "key4.db").AddQuotes() + " " + Path.Combine(datadir, "key4.db").AddQuotes());
ExecuteScript("copy " + Path.Combine(profiledir, "cookies.sqlite-wal").AddQuotes() + " " + Path.Combine(datadir, "cookies.sqlite-wal").AddQuotes());
ExecuteScript("copy " + Path.Combine(profiledir, "cookies.sqlite-shm").AddQuotes() + " " + Path.Combine(datadir, "cookies.sqlite-shm").AddQuotes());
        string json = ExecuteScript("python " + cookiescript + " " + (datadir + "\\cookies.sqlite").AddQuotes() + " " +
                                    hostfilter);
        json = "[" + json.SubstringAfter("[");
      //  ExecuteScript("del " + cookiepath + "");
        Console.WriteLine("JSON: " + json.Substring(0, 500) + "...");
        var cookies = JsonConvert.DeserializeObject<List<BrowserCookie>>(json);
        var cookiejar = new BrowserCookieJar();
        cookiejar.cookies = cookies;
        return cookiejar;
    }

    public static string GetCookie(bool forcestay = false)
    {
        if(forcestay)
        {
            InitCookies();
        }
        else{
        if (!hasinit)
        {
            hasinit = true;
            InitCookies();
        }
        }
        if (alphabetical)
        return AlphabeticallySortCookies(authcookie);
        else
        return authcookie;
    }

    public static void IncomingCookies(IEnumerable<String> headers)
    {
        Console.WriteLine("Incoming cookies.");
        var currentcookies = authcookie.Split("; ").Select(s => s.Split('=')[0].Trim()).ToList();
        var currentcookievalues = authcookie.Split("; ").Select(s => s.SubstringAfter("=").Trim()).ToList();
        headers = headers.Select(s => s.SubstringBefore(";").Trim()).ToList();
        foreach (var header in headers)
        {
            if(!currentcookies.Contains(header.Split('=')[0]))
            {
                if(header.Split(";")[0].SubstringAfter("=") != "EXPIRED")
                {
           currentcookies.Add(header.Split('=')[0].Trim());
           currentcookievalues.Add(header.Split(";")[0].SubstringAfter("="));
                }
            }
            else{
                if(header.Split(";")[0].SubstringAfter("=") != "EXPIRED")
                {
                var index = currentcookies.IndexOf(header.Split('=')[0].Trim());
                currentcookievalues[index] = header.Split(";")[0].SubstringAfter("=");
                }
                else{
                    var index = currentcookies.IndexOf(header.Split('=')[0].Trim());
                    currentcookies.Remove(header.Split('=')[0].Trim());
                    currentcookievalues.RemoveAt(index);
                }
            }
            Console.WriteLine(header);
        }
        var newcookie = "";
        for(int i = 0; i < currentcookies.Count; i++)
        {
            newcookie += currentcookies[i] + "=" + currentcookievalues[i] + "; ";
        }
        authcookie = newcookie;
        //Remove the last semicolon
        authcookie = authcookie.Substring(0, authcookie.Length - 2);

        Console.WriteLine("Cookie: " + authcookie);
        Console.WriteLine("End of Set-Cookie headers.");
        CookieValidate();
    }
    public static void CookieOvveride(string cookie)
    {
        authcookie = cookie;
    }
    public static void OvverideInit()
    {
        hasinit = true;
    }

    public static async Task<bool> CookieValidate()
    {
        if (SaveKeys.hasopened == false)
        {
            Console.WriteLine("This application has not been opened, cannot validate cookies, assuming valid.");
            return true;
        }

        Console.WriteLine("Cookie validation...");
        string url = JsonParsing.InitialReq(SaveKeys.lastopened, UrlConfig);
        return await NetworkManager.TestEndpoint(url);
    }

    public static void InitCookies(SaveKeys? keys = null)
    {
        SaveKeys = JsonParsing.GetSaveKeys();
        if (keys != null)
        {
            SaveKeys = keys.Value;
        }
        NetworkManager.SaveKeys = SaveKeys;
        UrlConfig = JsonParsing.GetUrlConfig();
        if (SaveKeys.ovveridecookie)
        {
            authcookie = SaveKeys.cookie;
            return;
        }
        if (SaveKeys.acceptedbrowserscraping == false)
        {
            Console.WriteLine("The user has not accepted browser scraping, cannot fetch cookies.");
            return;
        }
        else
        {
            var cookiejar = GetCookies();
            authcookie = FilterForGoogleDocsCookies(cookiejar);
        }
    }
    private static async Task<string> GetBrowserCookiePath()
    {
        if(browsercookiepath != "")
        {
            return browsercookiepath;
        }

        List<int> ValidIndexes = new List<int>();
        var browsers = JsonParsing.GetBrowserCookiePaths();
        Console.WriteLine("Browser cookie paths: ");
        foreach (var browser in browsers.browsers)
        {
            Console.WriteLine("- " + browser.name + ": " + browser.winpath + " (Windows), " + browser.linpath + " (Linux)");
        }
        Console.WriteLine("Validating browser cookie paths...");
        foreach (var browser in browsers.browsers)
        {
            if(OperatingSystem.IsWindows())
            {
                if(File.Exists(browser.winpath))
                {
                    ValidIndexes.Add(browsers.browsers.IndexOf(browser));
                    Console.WriteLine("Browser " + browser.name + " cookie path found: " + browser.winpath);
                }
            }
            else if(OperatingSystem.IsLinux())
            {
                if(File.Exists(browser.linpath))
                {
                    ValidIndexes.Add(browsers.browsers.IndexOf(browser));
                    Console.WriteLine("Browser " + browser.name + " cookie path found: " + browser.linpath);
                }
            }
        }
        if(ValidIndexes.Count == 0)
        {
            Console.WriteLine("No valid browser cookie paths found, requesting manual input.");
            ManualBrowserCookieInput();
            while(!CookieSelectorCallback)
            {
                await Task.Delay(100);
            }
            return browsercookiepath;
        }
        else if(ValidIndexes.Count == 1)
        {
            Console.WriteLine("One valid browser cookie path found, using " + browsers.browsers[ValidIndexes[0]].name);
            browsercookiepath = OperatingSystem.IsWindows() ? browsers.browsers[ValidIndexes[0]].winpath : browsers.browsers[ValidIndexes[0]].linpath;
            return browsercookiepath;
        }
        else
        {
            ShowBrowserSelector(ValidIndexes.ConvertAll(i => browsers.browsers[i].name));
            while(!CookieSelectorCallback)
            {
                await Task.Delay(100);
            }
            return browsercookiepath;
        }
    }
    public static void ManualInputCallback(string input)
    {
        browsercookiepath = input;
        CookieSelectorCallback = true;
    }
    private static void ManualBrowserCookieInput()
    {
     mainWindow.SetOpenManualInput(true);
     Console.WriteLine("Manual browser cookie path input requested.");
     Console.WriteLine("Waiting for user input...");
    }
    public static void PickBrowserCallback(string option)
    {
        var browsers = JsonParsing.GetBrowserCookiePaths();
        var browser = browsers.browsers.FirstOrDefault(b => b.name == option);
        if(/*browser != null*/true)
        {
            browsercookiepath = OperatingSystem.IsWindows() ? browser.winpath : browser.linpath;
            Console.WriteLine("Browser " + browser.name + " selected, using cookie path: " + browsercookiepath);
            CookieSelectorCallback = true;
        }
    }
    private static void ShowBrowserSelector(List<String> list)
    {
        mainWindow.SetPickOptions(list);
    }

    private static string FilterForGoogleDocsCookies(BrowserCookieJar browserCookieJar)
    {
        Console.WriteLine("Google Cookies: ");
        string tmpauthcookie = "";
        List<string> added = new List<string>();
        List<string> compassvalues = new List<string>();
        foreach (var cookie in browserCookieJar.cookies)
        {
            if (cookie.host == ".google.com" || cookie.host == ".docs.google.com" || cookie.host == "accounts.google.com" || cookie.host == "docs.google.com")
            {
                Console.WriteLine(cookie.name);
                if (cookie.value.Length > 50)
                {
                    Console.WriteLine("=" + cookie.value.Substring(0, 50) + "...");
                }
                else
                {
                    Console.WriteLine("=" + cookie.value);
                }

                if (SaveKeys.attachcookies.Contains(cookie.name))
                {
                    if (SaveKeys.enablemask)
                    {
                        if (!SaveKeys.mask[SaveKeys.attachcookies.IndexOf(cookie.name)])
                        {
                            continue;
                        }
                    }
                    if (cookie.name == "COMPASS")
                    {
                            Console.WriteLine("COMPASS cookie found. " + cookie.value);
                            if (!SaveKeys.compass.Contains(cookie.value.Split('=')[0]))
                            {
                                Console.WriteLine("COMPASS cookie not in config, skipping.");
                                continue;
                            }
                        else
                        {
                            if(compassvalues.Contains(cookie.value.Split('=')[0]))
                            {
                               Console.WriteLine("COMPASS cookie value " + cookie.value.Split('=')[0] + " already added, skipping.");
                                 continue; 
                            }
                            tmpauthcookie += cookie.name + "=" + cookie.value + "; ";
                            compassvalues.Add(cookie.value.Split('=')[0]);
                        }
                        continue;
                    }
                    if (!added.Contains(cookie.name))
{
    added.Add(cookie.name);
    tmpauthcookie += cookie.name + "=" + cookie.value + "; ";
}
else
{
    // Update to latest value
    var parts = tmpauthcookie.Split("; ").ToList();
    var idx = parts.FindIndex(p => p.StartsWith(cookie.name + "="));
    if (idx >= 0) parts[idx] = cookie.name + "=" + cookie.value;
    tmpauthcookie = string.Join("; ", parts);
}
                }
            }
        }

      /*  tmpauthcookie += "GFE_RTT=180; ";
        added.Add("GFE_RTT");*/

        //Remove the last semicolon
        tmpauthcookie = tmpauthcookie.Substring(0, tmpauthcookie.Length - 2);

        if (added == SaveKeys.attachcookies)
        {
            Console.WriteLine("All cookies found.");
        }
        else
        {
            foreach (var cookie in SaveKeys.attachcookies)
            {
                if (added.Contains(cookie) == false)
                {
                    Console.WriteLine("Cookie " + cookie + " not found, skipping.");
                }
                else
                {
                    Console.WriteLine("Cookie " + cookie + " found.");
                }
            }
        }
        Console.WriteLine(tmpauthcookie);
        foreach (var cookie in browserCookieJar.cookies)
{
    if (cookie.name == "HSID" || cookie.name == "SSID" || cookie.name == "SIDCC")
    {
        Console.WriteLine($"DEBUG {cookie.name} @ {cookie.host} = {cookie.value}");
    }
}
        return tmpauthcookie;
        }
        private static string AlphabeticallySortCookies(string cookie)
        {
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    return cookie;
                }

                var cookies = cookie.Split("; ", StringSplitOptions.RemoveEmptyEntries);
                Array.Sort(cookies, (left, right) =>
                {
                    string leftName = left.Split('=', 2)[0];
                    string rightName = right.Split('=', 2)[0];

                    if (leftName == "COMPASS")
                    {
                        return rightName == "COMPASS" ? 0 : -1;
                    }

                    if (rightName == "COMPASS")
                    {
                        return 1;
                    }

                    if (leftName == "OSID")
                    {
                        return rightName == "OSID" ? 0 : -1;
                    }

                    if (rightName == "OSID")
                    {
                        return 1;
                    }

                    return string.Compare(leftName, rightName, StringComparison.Ordinal);
                });

                return string.Join("; ", cookies);
        }
        private string GetRealPath(string path)
    {
        var oldpath = path;
        var browsercookiepathconfig = JsonParsing.GetBrowserCookiePaths();
        foreach(var key in browsercookiepathconfig.keys)
        {
            var substitute = key.key;
            switch(key.name)
            {
                case "PKFIRST":
               //replace all of the substitutes in the path with the first child 
                break;
                case "SYSUSER":
                oldpath = oldpath.Replace(substitute, Environment.UserName);
                break;
            }
        }
        return oldpath;
    }
}