using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;

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
    private static String currentUrl = "";
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private string doc_id = "";




    // Keep this in memory only; it is refreshed from the embedded login WebView.
    private string cookie = "";
    public GoogleDoc? doc;
    public MainWindow()
    {
        InitializeComponent();

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
    String url = GetBindReq(doc_id);
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
string url =  InitialReq(doc_id);
Console.WriteLine(url);
try
{


    string json = await GetRequest(url);
    Console.WriteLine(json);
  // JObject parsed = JObject.Parse(json.Split("&")[2]);
  string json2 = json.Split("3430&")[1];
  TryParseFirstJsonObject(json, out JObject? parsed);
  TryParseFirstJsonObject(json2, out JObject? parsed2);
  Console.WriteLine("JSON:");
   Console.WriteLine(parsed);
   Console.WriteLine("End JSON");
   Console.WriteLine("JSON2:");
   Console.WriteLine(parsed2);
   Console.WriteLine("End JSON2");
   Console.WriteLine("Raw:");
   Console.WriteLine(json);
   Console.WriteLine("End Raw");
   doc = new GoogleDoc(parsed, parsed2);
   Console.WriteLine("Doc created");
   /* if (json.Contains("[{\"ty\":\"is\",\"ibi\":1,\"s\":\""))
    {
        Console.WriteLine("JSON is valid");
        string last = json.Split("[{\"ty\":\"is\",\"ibi\":1,\"s\":\"")[1];
      //  Console.WriteLine(last);
        string doc = last.Split("\"},")[0].Replace("\\n","\n");
        MainText.Text = doc;
     //   Console.WriteLine(doc);
        BindToDoc();
    }
    else
    {
        MainText.Text = "JSON is invalid";
        Console.WriteLine("JSON is invalid");
    }*/
   MainText.Text = doc.GetText();
   BindToDoc();
}
catch (HttpRequestException err)
{
    Console.WriteLine(err.Message);
    MainText.Text = err.Message;
}
    }


    private async Task<string> GetRequest(string url)
    {
        var (statusCode, reasonPhrase, redirectLocation, body) = await SendRequestOnceAsync(url);

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            Console.WriteLine($"Received {(int)statusCode}. Retrying once with fresh cookie read...");
            await Task.Delay(200);
            (statusCode, reasonPhrase, redirectLocation, body) = await SendRequestOnceAsync(url);
        }

        Console.WriteLine($"Status: {(int)statusCode} {reasonPhrase}");
        if (redirectLocation is not null)
            Console.WriteLine($"Redirect to: {redirectLocation}");

        Console.WriteLine(body.Length > 500 ? body[..500] : body);

        if ((int)statusCode < 200 || (int)statusCode >= 300)
            throw new HttpRequestException($"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).");

        return body;
    }

    private async Task<(HttpStatusCode StatusCode, string? ReasonPhrase, Uri? RedirectLocation, string Body)> SendRequestOnceAsync(string url)
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

        return (response.StatusCode, response.ReasonPhrase, response.Headers.Location, body);
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


    private String InitialReq(String docid)
    {
        // Avoid stale hardcoded session/query parameters. Use a clean authenticated endpoint.
        return $"https://docs.google.com/document/d/{docid}/mobile/edit";
    }

    private String GetBindReq(String docid)
    {
        // Use only stable bind query parameters here. Session values must come from the current login session.
        return $"https://docs.google.com/document/d/{docid}/bind?id={docid}&includes_info_params=true&VER=8&c=1&w=1&RID=rpc&CI=0&AID=1&TYPE=xmlhttp";
    }
}