using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;

public static class Utils
{
    public static string AddQuotes(this string str)
    {
        return "\"" + str + "\"";
    }
    public static string SubstringAfter(this string str, string after)
    {
        int index = str.IndexOf(after, StringComparison.Ordinal);
        if (index == -1)
        {
            return "";
        }
        return str.Substring(index + after.Length);
    }
}
public struct BrowserCookie
{
    public String host;
    public String name;
    public String value;
}

public struct BrowserCookieJar
{
    public List<BrowserCookie> cookies;
}

public struct UrlConfig
{
    public String docidkey;
    public String bindurl;
    public String initurl;
}
public enum EditType { Insert, Alter, Multi,Noop,Unknown}
public class Edit
{
    public Edit(EditType type, String[] Params)
    {
        Type = type;
        this.Params = Params;
    }
    public Edit(JObject json)
    {
        String typestring = json["ty"].ToString();
        if (typestring == "is")
        {
            Type = EditType.Insert;
            String[] paramstmp = new string[2];
            paramstmp[0] = json["ibi"].ToString();
            paramstmp[1] = json["s"].ToString();
            Params = paramstmp;
        }
        else if (typestring == "as")
        {
            Type = EditType.Alter;
            String[] paramstmp = new string[0];
            Params = paramstmp;
        }
        else if (typestring == "ml") // Not sure code
        {
            Type = EditType.Multi;
            String[] paramstmp = new string[0];
            Params = paramstmp;
        }
        else if (typestring == "noop")
        {
            Type = EditType.Noop;
            String[] paramstmp = new string[0];
            Params = paramstmp;
        }
        else
        {
            Type = EditType.Unknown;
            String[] paramstmp = new string[0];
            Params = paramstmp;
        }

    }
    public EditType Type { get; set; }
    public String[] Params;
}
public class DocHistory
{
    public List<Edit> Edits { get; set; }

    public DocHistory(JObject json)
    {
        Edits = new List<Edit>();
        if (json is null) return;

        var edits = json["sc"] as JArray;
        if (edits is null) return;

        foreach (var token in edits)
        {
            if (token is JObject obj)
            {
                Edits.Add(new Edit(obj));
            }
        }
    }
    private static JObject? AsJObject(JToken? token)
    {
        if (token is null)
            return null;

        if (token.Type == JTokenType.Object)
            return (JObject)token;

        // Optional: wrap non\-object token into an object // return new JObject { ["value"] = token };

        return null;
    }
}
public class GoogleDoc
{
    JObject? json1;
    public DocHistory? history;

    public GoogleDoc(JObject json1, JObject json2)
    {
        this.json1 = json1;
        history = new DocHistory(json2);
    }

    public String GetText()
    {
        String content = "";
        foreach (var edit in history.Edits)
        {
            if (edit.Type == EditType.Insert)
            {
                if (Convert.ToInt32(edit.Params[0]) == content.Length + 1)
                {
                    content += edit.Params[1].Replace("\\n","\n");
                }
            }
        }
        return content;
    }
}
public partial class MainWindow : Window
{
    private UrlConfig UrlConfig;
    private static String currentUrl = "";
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private string doc_id = "";




    // Keep this in memory only; it is refreshed from the embedded login WebView.
    private string cookie = "COMPASS=documents=CmIACWuJV_M2JW4onWZwfk4_q-pk0A6MjLy8TBhTJSgU4Zt1Q47bhL85atCH9VapgZ5voLIDa6Ca2rArWhkl2pBFGn08S9N1MCBaR6x9l0yX0STANN-4yj4kg5dJKuN8jTJMqBC4lLTOBhqEAQAJa4lX47p7yIL65SC2cOvZPN82BYj6UQ11C3-PgqXLqgBHHkv7AIkkpa0o_FhCwFmSAbl4LDoYeQZGxTI87B5ALQQRbiHZXDkv-jyyE44AT-yTT3eiDlipMNQPPHpB7KO6WyQB98ttqF2ArzjJIaF385NGKmVNr92cCfP1DuAZhEPeQw==; NID=530=SN0yLQ4HEAFhgfGsg77skdcqlZLArLBeHUwQFOvYQXZ-rnzWrRTV2oJiiQKjro9Bbdd2Rwgo-zqclFZuiYilgrilOE6lizG1ZrrItQoY95fYiuduEOcDbtvB4BJ27y6zEZTZg8qBMUG5rNcd-6RpzjfyT01vsrkbyiZEHUgmp4pMy_PfDs4nTTgJpJq8OxFOAaJBy6hjfMb8xh8w-49VlIt1Vb0lxFv2aM5wQLWPMTpfc2YV-SS-xlGeToAZPBEKnp39geXpfWbdc0x6T4UPKIacvHJyh6PiblzbEYBYVAq3qD1pUViLxuWpkgw5HTSXqRAETG5nEbEcxH5IrqYRlD9rT_Kz8wAWnt5LU3eTv4zVpdN1LalxRNPKRv7XeWPDTviHRDtgnu46tS-W8AQmY3Xh6j97XDI6I-i8Ns16YoC59OYH6FPLO944uXMX6KCFuEJI0KMJTTS-VsdSuODGjZ69SmUYmuYb3e3PF75awrVeku_zYAwyFWmhhugQsekKD0a1XaGEATPWFF9Ia7z63PwFLGhO_-OS3KNmdpyuI8cyrHHEwF3xtIqx4d4HtwUzRFri-qkmHWyWbAykIQ6Ow-W2LVfw2eNH58C3gAhuqEPSKylOQ4kMPQOOkmaSYuByuFPh5Zi56LA3YEbPaJEuV_dOzh6d_p6TLA4UwMxNyWzEPZfaVmOBqqvuZuwBIREj1ogLajt0KqIhUG6K5pvi2Fb99KUhD3L31wv_CmpF3hzbdSlatkBSoFu601nOdk-NJWniZa6iu9EU0rFhuf2Dfbh2kHrqWYpLHvCKgHk-U5rAOWFslYlGNg; SID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLW2xS-mlmdSTxdkqK0speewACgYKAesSARQSFQHGX2MikkZ0ULg15goEz5t18SaJQhoVAUF8yKqw38t8PShDdOXclGh5Xqmd0076; __Secure-1PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsL6ZRevsd9Ur3JtMdtvR5KZQACgYKATcSARQSFQHGX2Mig7PWEJfanFzs3sU6xXAIRhoVAUF8yKpdpqwKBIJBxAVArr6POEMS0076; __Secure-3PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLkCixmOgc2FMTH30xZ4HPfgACgYKAQcSARQSFQHGX2MiKkC64r090tldq8PQdZibGhoVAUF8yKpNnBAHaospDvlKRWIcqFnm0076; HSID=Ar3yC1mBUaa1a0Fpp; SSID=AYMr1tbyU9avtPX4-; APISID=ju4fB_YNuGgBT_Pp/AOm8LL5biEbGkOgc7; SAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-1PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-3PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; SIDCC=AKEyXzWlNlMRvES5s0VflPPUK2PhYouanpKAHreZL4KtSQZPTBDG2VDgxiHpV-yGR4MTlcHfwuA; __Secure-1PSIDCC=AKEyXzWy_62-91JO99hkQHvUFvgcrDdfbZGFe8rWpIDTDrS21TTSOPnIYrxtFQEQBiOh3YjLn2k; __Secure-3PSIDCC=AKEyXzVsVKbCVdC_LksLNRUqnuI5YvruuJo-hIpXbKbfNeCXDJTJCyq5ztmg_riI5MCEq4oGsaKJ; SEARCH_SAMESITE=CgQIxp8B; __Secure-1PSIDTS=sidts-CjEBWhotCTZWm83kA9ZY2inOhquktS_lVv8PY9RzeO0rKzwjcZ9OaScvZiXdFnzJG0GGEAA; __Secure-3PSIDTS=sidts-CjEBWhotCTZWm83kA9ZY2inOhquktS_lVv8PY9RzeO0rKzwjcZ9OaScvZiXdFnzJG0GGEAA; OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYsCuqTMhL8n40u37mc4dxpwACgYKAeESARQSFQHGX2Mi1jzy-75FcNkhkBuiqj1WtBoVAUF8yKoK_-dbuCdEhcpq2O0dHC3A0076; __Secure-OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYYTQ51tXRsmSQmEs5JHUkagACgYKASQSARQSFQHGX2MiHRqlE2y_S-eTYgU0SkXnzhoVAUF8yKoXponDY9K9aaaLyKLM36DV0076; __Secure-BUCKET=CLQC; AEC=AaJma5v23HIs1lldTWaW9Jk1pb9DgsXdLIOncmDNe3ZqBTcz9mJf0jf3huE; COMPASS=appsfrontendserver=CgAQ9M-pzgYafQAJa4lXNdK39218o12B2cPkLXlou9FGt4_7JcDYZ4-HgnOHpsw9ESh_LGoS2iw4ufcm7Le-EAe9qg2jQ_FjVcOYONRpfKTJK8oZUyehGLcAy5fnou7Xgkj9ZEGzM0PLMVq2kDG3lNned2sjr2ps1QJr6JHPPX7wCEiOThdXIAEwAQ; S=billing-ui-v3=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw:billing-ui-v3-efe=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw";   public GoogleDoc? doc;
    private string cookiescript = "../../../browser_cookies.py";
    public MainWindow()
    {
        InitializeComponent();
        UrlConfig = GetUrlConfig();
        //Console.WriteLine(GetCookies().ToString());
    }















    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        doc_id = docidbox.Text;
    }

    private Stream BindRequest(String url)
    {
        return client.GetStreamAsync(url).Result;
    }

    private async void BindToDoc()
    {
    String url = GetBindReq(doc_id,UrlConfig);
    Console.WriteLine(url);
    while (true)
    {
        Console.WriteLine("Binding...");
        Stream jsonstream = BindRequest(url);
        StreamReader jsonreader = new StreamReader(jsonstream);
        String blocksize = "";
        while (true)
        {
            if (jsonreader.Peek() == 38)
            {
                jsonreader.Read();
                break;
            }
            blocksize += (char)jsonreader.Read();
        }
        int blocksizeint = Convert.ToInt32(blocksize);
        char[] buffer = new char[blocksizeint];
        jsonreader.Read(buffer, 0, blocksizeint);
        string json = new string(buffer);
        Console.WriteLine("JSON: " + json);
                if (json.Contains("noop"))
                {
                    doc.history.Edits.Add(new Edit(EditType.Noop,new string[0]));
                    continue;
                }
                if (TryParseFirstJsonObject(json, out JObject? obj) && obj is not null)
                {
                    doc.history.Edits.Add(new Edit(obj));
                }
        MainText.Text = doc.GetText();
        Console.WriteLine(json);
    }
    }

    private async void OpenDoc(object? sender, RoutedEventArgs e)
    {
MainText.Text = "Loading...";
Console.WriteLine("Loading...");
string url =  InitialReq(doc_id,UrlConfig);
Console.WriteLine(url);
try
{


    string json = await GetRequest(url);

  if (!TryExtractFirstJsonObject(json, out JObject? parsed, out int firstStart, out int firstEnd, out string rawFirst))
  {
      MainText.Text = "Failed to parse first JSON object";
      return;
  }

  if (!TryReadLengthPrefixedSegment(json, firstEnd, out string json2Raw, out int _))
  {
      MainText.Text = "Failed to parse length-prefixed second segment";
      return;
  }

// Optional sanity log (rawFirst is exact substring from server)
  Console.WriteLine($"First JSON chars: {rawFirst.Length}");
  Console.WriteLine($"Second segment chars: {json2Raw.Length}");

// If json2Raw itself contains wrappers, extract first object from it:
  if (!TryExtractFirstJsonObject(json2Raw, out JObject? parsed2, out _, out _, out _))
  {
      MainText.Text = "Failed to parse second JSON object";
      return;
  }

  doc = new GoogleDoc(parsed!, parsed2!);
  MainText.Text = doc.GetText();
  BindToDoc();

}
catch (HttpRequestException err)
{
    Console.WriteLine(err.Message);
    MainText.Text = err.Message;
}
    }

// Exact extraction from raw text: finds first {...} while handling nested braces and quoted strings.
private static bool TryExtractFirstJsonObject(
    string input,
    out JObject? obj,
    out int startIndex,
    out int endIndexExclusive,
    out string rawJson)
{
    obj = null;
    startIndex = -1;
    endIndexExclusive = -1;
    rawJson = string.Empty;

    if (string.IsNullOrEmpty(input))
        return false;

    int i = 0;
    while (i < input.Length && input[i] != '{') i++;
    if (i >= input.Length) return false;

    startIndex = i;

    int depth = 0;
    bool inString = false;
    bool escape = false;

    for (; i < input.Length; i++)
    {
        char c = input[i];

        if (inString)
        {
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = false;
            }

            continue;
        }

        if (c == '"')
        {
            inString = true;
            continue;
        }

        if (c == '{')
        {
            depth++;
            continue;
        }

        if (c == '}')
        {
            depth--;
            if (depth == 0)
            {
                endIndexExclusive = i + 1;
                rawJson = input.Substring(startIndex, endIndexExclusive - startIndex);

                try
                {
                    obj = JObject.Parse(rawJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    return false;
}

private async void CookieValidate()
{
    Console.WriteLine("Cookie validation...");
    string url =  InitialReq(doc_id,UrlConfig);
    await GetRequest(url);
}
// Reads: [&]digits&payload  where payload length is exactly 'digits'.
private static bool TryReadLengthPrefixedSegment(
    string input,
    int startIndex,
    out string payload,
    out int nextIndex)
{
    payload = string.Empty;
    nextIndex = startIndex;

    if (input is null || startIndex < 0 || startIndex >= input.Length)
        return false;

    int i = startIndex;

    // Optional whitespace
    while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

    // Optional leading '&'
    if (i < input.Length && input[i] == '&') i++;

    int lenStart = i;
    while (i < input.Length && char.IsDigit(input[i])) i++;

    if (i == lenStart) return false; // no digits
    if (i >= input.Length || input[i] != '&') return false;

    if (!int.TryParse(input.Substring(lenStart, i - lenStart), out int length) || length < 0)
        return false;

    i++; // skip '&' before payload

    if (i + length > input.Length) return false;

    payload = input.Substring(i, length);
    nextIndex = i + length;
    return true;
}

    private async Task<string> GetRequest(string url)
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
            cookie = "";
            foreach (var header in headers.GetValues("Set-Cookie"))
            {
                cookie += header.Split(';')[0] + "; ";
                Console.WriteLine(header);
            }
            Console.WriteLine("Cookie: " + cookie);
            Console.WriteLine("End of Set-Cookie headers.");
            CookieValidate();
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

    private async Task<(HttpStatusCode StatusCode, string? ReasonPhrase, Uri? RedirectLocation, string Body, HttpResponseHeaders headers)> SendRequestOnceAsync(string url)
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


            request.Headers.TryAddWithoutValidation("Cookie", cookie);
            Console.WriteLine("Attached auth cookies to request.");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Referer", "https://docs.google.com/");

        using var response = await localClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();


        return (response.StatusCode, response.ReasonPhrase, response.Headers.Location, body,response.Headers);
    }





    private static bool TryParseFirstJsonObject(string input, out JObject? obj)
    {
     obj = null;
     if (string.IsNullOrWhiteSpace(input)) return false;

     var start = input.IndexOf('{');
     if (start <0) return false;

     var jsonPart = input[start..];

     using var sr = new StringReader(jsonPart);
     using var reader = new JsonTextReader(sr)
     {
     SupportMultipleContent = true };

     var serializer = JsonSerializer.CreateDefault();

     while (reader.Read())
     {
     if (reader.TokenType == JsonToken.StartObject)
     {
     obj = serializer.Deserialize<JObject>(reader);
     return obj is not null;
     }
     }

     return false;
    }


    private String InitialReq(String docid,UrlConfig config)
    {
        return config.initurl.Replace(config.docidkey, docid);
    }

    private String GetBindReq(String docid,UrlConfig config)
    {
        return config.bindurl.Replace(config.docidkey, docid);
    }

    private String ExecuteScript(String cmd)
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

    private UrlConfig GetUrlConfig()
    {
        string json = File.ReadAllText("../../../UrlConfig.json");
        var UrlConfig = JsonConvert.DeserializeObject<UrlConfig>(json);
        return UrlConfig;
    }



    private BrowserCookieJar GetCookies(String hostfilter = "")
    {
        string datadir = "C:\\Users\\nolan\\AppData\\Roaming\\GoogleDocs";
        Console.WriteLine(ExecuteScript("mkdir " + datadir));
        string cookiepath = "C:\\Users\\nolan\\AppData\\Roaming\\zen\\Profiles\\us8cxx3x.Default (alpha)\\cookies.sqlite";
        string cpcmd = "copy " + cookiepath.AddQuotes() + " " + (datadir + "\\cookies.sqlite").AddQuotes();
        Console.WriteLine(cpcmd);
        Console.WriteLine(ExecuteScript(cpcmd));
        cookiepath += ".docbackup";
        string json = ExecuteScript("python " + cookiescript + " " + (datadir + "\\cookies.sqlite").AddQuotes() + " " + hostfilter);
        json = "[" + json.SubstringAfter("[");
        ExecuteScript("del " + cookiepath + "");
        Console.WriteLine("JSON: " + json.Substring(0, 500) + "...");
        var cookies = JsonConvert.DeserializeObject<List<BrowserCookie>>(json);
        var cookiejar = new BrowserCookieJar();
        cookiejar.cookies = cookies;
        return cookiejar;
    }
}