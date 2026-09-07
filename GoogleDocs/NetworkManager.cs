using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDocs;

public static class NetworkManager
{
    public static string sid = "";
    public static string ouid = "";
    public static SaveKeys SaveKeys { get; set; } = new();
    public static MainWindow loggingdest;
    private static void PrintDebugMenu(string s)
    {
        if (loggingdest != null)
        {
            loggingdest.PrintDebugMenu(s);
        }
    }

    private static void PrintLineDebugMenu(string s)
    {
        PrintDebugMenu(s + "\n");
    }
    private static string SanitizeCookieHeader(string cookie)
    {
        if (string.IsNullOrEmpty(cookie))
        {
            return string.Empty;
        }

        // RFC-compliant header values must be ASCII; strip control and non-ASCII chars.
        var filtered = new char[cookie.Length];
        int write = 0;
        foreach (char c in cookie)
        {
            if (c >= 32 && c <= 126)
            {
                filtered[write++] = c;
            }
        }

        return new string(filtered, 0, write);
    }
    public static async Task<string> PostRequest(string url,string postdata = "",bool bypassattachments = false)
    {
        if (!bypassattachments)
        {
            if (sid != "" && !url.Contains("sid="))
            {
                PrintLineDebugMenu($"Appending sid to URL: {sid}");
                if (url.Contains("?") == false)
                {
                    url += $"?sid={sid}";
                }
                else
                {
                    url += $"&sid={sid}";
                }

                PrintLineDebugMenu($"Updated URL: {url}");
            }

           /* if (ouid != "" && !url.Contains("ouid="))
            {
                PrintLineDebugMenu($"Appending ouid to URL: {ouid}");
                if (url.Contains("?") == false)
                {
                    url += $"?ouid={ouid}";
                }
                else
                {
                    url += $"&ouid={ouid}";
                }

                PrintLineDebugMenu($"Updated URL: {url}");
            }*/

        }

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };

        using var localClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (postdata != "")
        {
           request.Content = new StringContent(postdata);
           request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded;charset=utf-8");
        //   request.Headers.Add("Content-Type", "application/x-www-form-urlencoded;charset=utf-8");
           request.Content.Headers.Add("Content-Length", postdata.Length.ToString());
        }

        // Build cookies for this exact URL from WebView2 cookie jar
        var rawCookie = await CookieManager.GetCookie();
        var sanitizedCookie = SanitizeCookieHeader(rawCookie);
        if (string.IsNullOrWhiteSpace(sanitizedCookie))
        {
            throw new HttpRequestException("Cookie header is empty after sanitization.");
        }

        if (!string.Equals(rawCookie, sanitizedCookie, StringComparison.Ordinal))
        {
            PrintLineDebugMenu("Cookie header contained non-ASCII or control characters; sanitized before request.");
            PrintDifferences(rawCookie, sanitizedCookie);
        }

        request.Headers.Add("Cookie", sanitizedCookie);
        PrintLineDebugMenu("Attached auth cookies to request.");
        request.Headers.Add("User-Agent", "UnofficialGoogleDocs/1.0");
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Referer", "https://docs.google.com/");
        request.Headers.Add("Origin", "https://docs.google.com");
        request.Headers.Add("X-Same-Domain", "1");
        PrintHttpRequestData(request);
        using var response = await localClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var headers = response.Headers;
        if(headers.Contains("reporting-endpoints"))
        {
            PrintLineDebugMenu("Found reporting-endpoints header:");
            foreach(var val in headers.GetValues("reporting-endpoints"))
            {
                foreach(var part in val.Split('&'))
                {
                    if(part.StartsWith("sid="))
                    {
                        PrintLineDebugMenu("Found sid in reporting-endpoints header.");
                        sid = part.SubstringAfter("sid=");
                        PrintLineDebugMenu($"Extracted sid: {sid}");
                    }
                    if(part.StartsWith("ouid="))
                    {
                        PrintLineDebugMenu("Found ouid in reporting-endpoints header.");
                        ouid = part.SubstringAfter("ouid=");
                        PrintLineDebugMenu($"Extracted ouid: {ouid}");
                    }
                }
                PrintLineDebugMenu(val);
            }

        }
        else
        {
            PrintLineDebugMenu("No reporting-endpoints header found.");
        }
        if (headers.Contains("Set-Cookie"))
        {
            PrintLineDebugMenu("Found Set-Cookie header.");
            CookieManager.IncomingCookies(headers.GetValues("Set-Cookie"));
        }
        if (response.StatusCode == HttpStatusCode.Found)
        {
            PrintLineDebugMenu("Found Found Page");
            PrintLineDebugMenu($"Redirecting to {response.Headers.Location.AbsoluteUri}...");
            return await GetRequest(response.Headers.Location.AbsoluteUri);
        }
        PrintLineDebugMenu($"POST REQ RETURNED: {response.StatusCode}");
            return body;
    }

    public static async IAsyncEnumerable<String> GetStreamAsync(string url,[EnumeratorCancellation] CancellationToken cancel,bool bypassattachments = false)
    {
        if (!bypassattachments)
        {
            if (sid != "" && !url.Contains("sid="))
            {
                if (url.Contains("?") == false)
                {
                    url += $"?sid={sid}";
                }
                else
                {
                    url += $"&sid={sid}";
                }

                PrintLineDebugMenu($"Updated URL with sid: {url}");
            }

            if (ouid != "" && !url.Contains("ouid="))
            {
                if (url.Contains("?") == false)
                {
                    url += $"?ouid={ouid}";
                }
                else
                {
                    url += $"&ouid={ouid}";
                }

                PrintLineDebugMenu($"Updated URL with ouid: {url}");
            }
        }

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };

        using var localClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var rawCookie = await CookieManager.GetCookie();
        var sanitizedCookie = SanitizeCookieHeader(rawCookie);
        if (string.IsNullOrWhiteSpace(sanitizedCookie))
        {
            throw new HttpRequestException("Cookie header is empty after sanitization.");
        }

        if (!string.Equals(rawCookie, sanitizedCookie, StringComparison.Ordinal))
        {
            PrintLineDebugMenu("Cookie header contained non-ASCII or control characters; sanitized before request.");
            PrintDifferences(rawCookie, sanitizedCookie);
        }

        request.Headers.Add("Cookie", sanitizedCookie);
        PrintLineDebugMenu("Attached auth cookies to request.");
        request.Headers.Add("User-Agent", "UnofficialGoogleDocs/1.0");
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Referer", "https://docs.google.com/");
        request.Headers.Add("X-Same-Domain", "1");
        HttpResponseMessage response = await localClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        PrintLineDebugMenu($"Response status code: {(int)response.StatusCode} ({response.ReasonPhrase})");
        PrintLineDebugMenu("GET STREAM RETURNED");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            PrintLineDebugMenu($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
            PrintLineDebugMenu($"Response headers: {string.Join(", ", response.Headers)}");
            PrintLineDebugMenu($"Body: {await response.Content.ReadAsStringAsync()}");
        }

        //return await response.Content.ReadAsStreamAsync();
       // return response.Content.ReadAsStream();
       var stream = await response.Content.ReadAsStreamAsync();
       var reader = new StreamReader(stream);
       while (cancel.IsCancellationRequested == false)
       {

        /*   int opens = 0;
           int closes = 0;

           while (reader.Peek() != -1)
           {
               char c = (char)reader.Read();
Console.Write(c);
               if (opens > closes)
               {
                   x += c;
               }
               if (IsOpen(c))
               {
                   opens++;
                   x += c;
               }

               if (IsClose(c))
               {
                   closes++;
               }

           }*/
        string x = "";
        string countstr = "";
        while (true)
        {
            if (reader.Peek() != -1)
            {
                char c = ' ';
                while (true)
                {
                    c = (char)reader.Read();
                    if (IsNumber(c))
                    {
                        break;
                    }
                }

                while (IsNumber(c))
                {
                    countstr += c;
                    c = (char)reader.Read();
                }

                int count = int.Parse(countstr);
                for (int i = 0; i < count; i++)
                {
                    x += c;
                    c = (char)reader.Read();
                }

                break;
            }
            else
            {
                await Task.Delay(50);
            }
        }

        yield return x;
       }
    }

    private static bool IsNumber(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool IsOpen(char c)
    {
        return c == '[' || c == '{';
    }
    private static bool IsClose(char c)
    {
        return c == ']' || c == '}';
    }
     public static async Task<string> GetRequest(string url,bool bypassattachments = false)
    {
         if(sid != "" && !bypassattachments)
        {
            if (url.Contains("?") == false)
            {
                url += $"?sid={sid}";
            }
            else
            {
                url += $"&sid={sid}";
            }

            PrintLineDebugMenu($"Updated URL with sid: {url}");
        }     
        
        var (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);

       

        PrintLineDebugMenu("Headers:");
        foreach (var header in headers)
        {
            PrintLineDebugMenu($"{header.Key}: {header.Value.Aggregate((string a, string b) => { return a + ", " + b;})}");
        }
if(headers.Contains("reporting-endpoints"))
        {
            PrintLineDebugMenu("Found reporting-endpoints header:");
            foreach(var val in headers.GetValues("reporting-endpoints"))
            {
               foreach(var part in val.Split('&'))
                {
                    if(part.StartsWith("sid="))
                    {
                        PrintLineDebugMenu("Found sid in reporting-endpoints header.");
                        sid = part.SubstringAfter("sid=");
                        PrintLineDebugMenu($"Extracted sid: {sid}");
                    }
                    if(part.StartsWith("ouid="))
                    {
                        PrintLineDebugMenu("Found ouid in reporting-endpoints header.");
                        ouid = part.SubstringAfter("ouid=");
                        PrintLineDebugMenu($"Extracted ouid: {ouid}");
                    }
                }
                PrintLineDebugMenu(val);
            }   
               
        }
        else
        {
            PrintLineDebugMenu("No reporting-endpoints header found.");
        }
        if (headers.Contains("Set-Cookie"))
        {
            PrintLineDebugMenu("Found Set-Cookie header.");
           CookieManager.IncomingCookies(headers.GetValues("Set-Cookie"));
        }
        
         if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            PrintLineDebugMenu($"Received {(int)statusCode}. Retrying once with fresh cookie read...");
            await Task.Delay(200);
            (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);
        }
        PrintLineDebugMenu("End of headers.");

        PrintLineDebugMenu($"Status: {(int)statusCode} {reasonPhrase}");
        if (redirectLocation is not null)
            PrintLineDebugMenu($"Redirect to: {redirectLocation}");

        PrintLineDebugMenu(body.Length > 500 ? body[..500] : body);

        if ((int)statusCode < 200 || (int)statusCode >= 300)
            throw new HttpRequestException($"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).");


        
        return body;
    }

    public static async Task<bool> TestEndpoint(string url)
    {
        var (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);
        if(statusCode != HttpStatusCode.OK)
        {
            PrintLineDebugMenu($"Test endpoint returned {(int)statusCode} {reasonPhrase}");
        }
        else
        {
            PrintLineDebugMenu($"Test endpoint returned {(int)statusCode} {reasonPhrase}");
        }
        return statusCode == HttpStatusCode.OK;
    }


    private static async Task<(HttpStatusCode StatusCode, string? ReasonPhrase, Uri? RedirectLocation, string Body, HttpResponseHeaders headers)> SendRequestOnceAsync(string url, bool completeEarly = false)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        using var localClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Build cookies for this exact URL from WebView2 cookie jar
        var rawCookie = await CookieManager.GetCookie();
        var sanitizedCookie = SanitizeCookieHeader(rawCookie);
        if (string.IsNullOrWhiteSpace(sanitizedCookie))
        {
            throw new HttpRequestException("Cookie header is empty after sanitization.");
        }

        if (!string.Equals(rawCookie, sanitizedCookie, StringComparison.Ordinal))
        {
            PrintLineDebugMenu("Cookie header contained non-ASCII or control characters; sanitized before request.");
            PrintDifferences(rawCookie, sanitizedCookie);
        }

        request.Headers.Add("Cookie", sanitizedCookie);
        PrintLineDebugMenu("Attached auth cookies to request.");
        request.Headers.Add("User-Agent", "UnofficialGoogleDocs/1.0");
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Referer", "https://docs.google.com/");

        PrintHttpRequestData(request);
        using var response = completeEarly ? await localClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead) : await localClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Found)
        {
            PrintLineDebugMenu("Found Found Page");
            PrintLineDebugMenu($"Redirecting to {response.Headers.Location.AbsoluteUri}...");
            return await SendRequestOnceAsync(response.Headers.Location.AbsoluteUri);
        }
            return (response.StatusCode, response.ReasonPhrase, response.Headers.Location, body, response.Headers);
    }
    private static void PrintHttpRequestData(HttpRequestMessage msg)
    {
        //include url, headers, and body

            PrintLineDebugMenu($"Request URL: {msg.RequestUri}");
            PrintLineDebugMenu("Headers:");
            foreach (var header in msg.Headers)
            {
                PrintLineDebugMenu($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            PrintLineDebugMenu("");
            PrintLineDebugMenu("Body:");
            if (msg.Content != null)
            {
                var body = msg.Content.ReadAsStringAsync().Result;
                PrintLineDebugMenu(body);
            }
            else
            {
                PrintLineDebugMenu("No body content.");
            }
    }
    public static void PrintDifferences(string oldText, string newText)
    {
        int n = oldText.Length;
        int m = newText.Length;
        int[,] dp = new int[n + 1, m + 1];

        // Fill the DP table for Longest Common Subsequence
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (oldText[i - 1] == newText[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        // Backtrack to find the diffs
        int row = n, col = m;
        var diffResult = new System.Collections.Generic.List<(char character, ConsoleColor color)>();

        while (row > 0 || col > 0)
        {
            if (row > 0 && col > 0 && oldText[row - 1] == newText[col - 1])
            {
                // No change
                diffResult.Insert(0, (oldText[row - 1], ConsoleColor.Gray));
                row--; col--;
            }
            else if (col > 0 && (row == 0 || dp[row, col - 1] >= dp[row - 1, col]))
            {
                // Insertion (present in new, not in old)
                diffResult.Insert(0, (newText[col - 1], ConsoleColor.Green));
                col--;
            }
            else if (row > 0 && (col == 0 || dp[row, col - 1] < dp[row - 1, col]))
            {
                // Deletion (present in old, not in new)
                diffResult.Insert(0, (oldText[row - 1], ConsoleColor.Red));
                row--;
            }
        }

        // Print the result to console
        foreach (var (character, color) in diffResult)
        {
            Console.ForegroundColor = color;
            Console.Write(character);
        }

        Console.ResetColor();
        PrintLineDebugMenu("");
    }
    public static string GetSid()
    {
        return sid;
    }

}