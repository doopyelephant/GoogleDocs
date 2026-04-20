using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GoogleDocs;

public static class NetworkManager
{
    public static SaveKeys SaveKeys { get; set; } = new();

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

     public static async Task<string> GetRequest(string url)
    {
        var (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);

       

        Console.WriteLine("Headers:");
        foreach (var header in headers)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }

        if (headers.Contains("Set-Cookie"))
        {
            Console.WriteLine("Found Set-Cookie header.");
           CookieManager.IncomingCookies(headers.GetValues("Set-Cookie"));
        }
         if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            Console.WriteLine($"Received {(int)statusCode}. Retrying once with fresh cookie read...");
            await Task.Delay(200);
            (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);
        }
        Console.WriteLine("End of headers.");

        Console.WriteLine($"Status: {(int)statusCode} {reasonPhrase}");
        if (redirectLocation is not null)
            Console.WriteLine($"Redirect to: {redirectLocation}");

        Console.WriteLine(body.Length > 500 ? body[..500] : body);

        if ((int)statusCode < 200 || (int)statusCode >= 300)
            throw new HttpRequestException($"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).");


        
        return body;
    }

    public static async Task<bool> TestEndpoint(string url)
    {
        var (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);
        if(statusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Test endpoint returned {(int)statusCode} {reasonPhrase}");
        }
        return statusCode == HttpStatusCode.OK;
    }


    private static async Task<(HttpStatusCode StatusCode, string? ReasonPhrase, Uri? RedirectLocation, string Body, HttpResponseHeaders headers)> SendRequestOnceAsync(string url)
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
        var rawCookie = CookieManager.GetCookie();
        var sanitizedCookie = SanitizeCookieHeader(rawCookie);
        if (string.IsNullOrWhiteSpace(sanitizedCookie))
        {
            throw new HttpRequestException("Cookie header is empty after sanitization.");
        }

        if (!string.Equals(rawCookie, sanitizedCookie, StringComparison.Ordinal))
        {
            Console.WriteLine("Cookie header contained non-ASCII or control characters; sanitized before request.");
            PrintDifferences(rawCookie, sanitizedCookie);
        }

        request.Headers.TryAddWithoutValidation("Cookie", sanitizedCookie);
        Console.WriteLine("Attached auth cookies to request.");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Referer", "https://docs.google.com/");

        PrintHttpRequestData(request);
        using var response = await localClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Found)
        {
            Console.WriteLine("Found Found Page");
            Console.WriteLine($"Redirecting to {response.Headers.Location.AbsoluteUri}...");
            return await SendRequestOnceAsync(response.Headers.Location.AbsoluteUri);
        }
            return (response.StatusCode, response.ReasonPhrase, response.Headers.Location, body, response.Headers);
    }
    private static void PrintHttpRequestData(HttpRequestMessage msg)
    {
        //include url, headers, and body

            Console.WriteLine($"Request URL: {msg.RequestUri}");
            Console.WriteLine("Headers:");
            foreach (var header in msg.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            Console.WriteLine("");
            Console.WriteLine("Body:");
            if (msg.Content != null)
            {
                var body = msg.Content.ReadAsStringAsync().Result;
                Console.WriteLine(body.Length > 500 ? body[..500] : body);
            }
            else
            {
                Console.WriteLine("No body content.");
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
        Console.WriteLine();
    }

}