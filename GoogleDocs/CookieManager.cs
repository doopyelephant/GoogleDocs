using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private static string ExecuteScript(string cmd)
    {
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c " + cmd; // Replace 'dir' with your command
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

// Read the output stream
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private static BrowserCookieJar GetCookies(string hostfilter = "")
    {
        string datadir = "C:\\Users\\nolan\\AppData\\Roaming\\GoogleDocs";
        Console.WriteLine(ExecuteScript("mkdir " + datadir));
        string cookiepath =
            "C:\\Users\\nolan\\AppData\\Roaming\\zen\\Profiles\\us8cxx3x.Default (alpha)\\cookies.sqlite";
        string cpcmd = "copy " + cookiepath.AddQuotes() + " " + (datadir + "\\cookies.sqlite").AddQuotes();
        Console.WriteLine(cpcmd);
        Console.WriteLine(ExecuteScript(cpcmd));
        cookiepath += ".docbackup";
        string json = ExecuteScript("python " + cookiescript + " " + (datadir + "\\cookies.sqlite").AddQuotes() + " " +
                                    hostfilter);
        json = "[" + json.SubstringAfter("[");
        ExecuteScript("del " + cookiepath + "");
        Console.WriteLine("JSON: " + json.Substring(0, 500) + "...");
        var cookies = JsonConvert.DeserializeObject<List<BrowserCookie>>(json);
        var cookiejar = new BrowserCookieJar();
        cookiejar.cookies = cookies;
        return cookiejar;
    }

    public static string GetCookie()
    {
        if (!hasinit)
        {
            hasinit = true;
            InitCookies();
        }

        return AlphabeticallySortCookies(authcookie);
    }

    public static void IncomingCookies(IEnumerable<String> headers)
    {
        Console.WriteLine("Incoming cookies.");
        authcookie = "";
        foreach (var header in headers)
        {
            authcookie += header.Split(';')[0] + "; ";
            Console.WriteLine(header);
        }

        Console.WriteLine("Cookie: " + authcookie);
        Console.WriteLine("End of Set-Cookie headers.");
        CookieValidate();
    }

    private static async Task<bool> CookieValidate()
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
        GetBrowserCookiePath();
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
    private static void ShowBrowserSelector(List<String> list)
    {
        
    }

    private static string FilterForGoogleDocsCookies(BrowserCookieJar browserCookieJar)
    {
        Console.WriteLine("Google Cookies: ");
        string tmpauthcookie = "";
        List<string> added = new List<string>();
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

                    }
                    if (!added.Contains(cookie.name))
                    {
                        tmpauthcookie += cookie.name + "=" + cookie.value + "; ";
                        added.Add(cookie.name);
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
        return tmpauthcookie;
        }
        private static string AlphabeticallySortCookies(string cookie)
        {
            var cookies = cookie.Split("; ");
            Array.Sort(cookies);
            return string.Join("; ", cookies);
        }
}