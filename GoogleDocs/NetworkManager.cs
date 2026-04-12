using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GoogleDocs;

public static class NetworkManager
{
    public static SaveKeys SaveKeys { get; set; } = new();

     public static async Task<string> GetRequest(string url)
    {
        var (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            Console.WriteLine($"Received {(int)statusCode}. Retrying once with fresh cookie read...");
            await Task.Delay(200);
            (statusCode, reasonPhrase, redirectLocation, body, headers) = await SendRequestOnceAsync(url);
        }

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


            request.Headers.TryAddWithoutValidation("Cookie", CookieManager.GetCookie());
            Console.WriteLine("Attached auth cookies to request.");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Referer", "https://docs.google.com/");

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

}