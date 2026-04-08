using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;




public partial class MainWindow : Window
{
    private UrlConfig UrlConfig;
    private BrowserCookiePaths browsercookiepaths;
    private static String currentUrl = "";
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private string doc_id = "";
    private const int urlconfig_version = 1;
    private const int browsercookiepath_version = 1;


    // Keep this in memory only; it is refreshed from the embedded login WebView.
    private string cookie = "COMPASS=documents=CmIACWuJV_M2JW4onWZwfk4_q-pk0A6MjLy8TBhTJSgU4Zt1Q47bhL85atCH9VapgZ5voLIDa6Ca2rArWhkl2pBFGn08S9N1MCBaR6x9l0yX0STANN-4yj4kg5dJKuN8jTJMqBC4lLTOBhqEAQAJa4lX47p7yIL65SC2cOvZPN82BYj6UQ11C3-PgqXLqgBHHkv7AIkkpa0o_FhCwFmSAbl4LDoYeQZGxTI87B5ALQQRbiHZXDkv-jyyE44AT-yTT3eiDlipMNQPPHpB7KO6WyQB98ttqF2ArzjJIaF385NGKmVNr92cCfP1DuAZhEPeQw==; NID=530=SN0yLQ4HEAFhgfGsg77skdcqlZLArLBeHUwQFOvYQXZ-rnzWrRTV2oJiiQKjro9Bbdd2Rwgo-zqclFZuiYilgrilOE6lizG1ZrrItQoY95fYiuduEOcDbtvB4BJ27y6zEZTZg8qBMUG5rNcd-6RpzjfyT01vsrkbyiZEHUgmp4pMy_PfDs4nTTgJpJq8OxFOAaJBy6hjfMb8xh8w-49VlIt1Vb0lxFv2aM5wQLWPMTpfc2YV-SS-xlGeToAZPBEKnp39geXpfWbdc0x6T4UPKIacvHJyh6PiblzbEYBYVAq3qD1pUViLxuWpkgw5HTSXqRAETG5nEbEcxH5IrqYRlD9rT_Kz8wAWnt5LU3eTv4zVpdN1LalxRNPKRv7XeWPDTviHRDtgnu46tS-W8AQmY3Xh6j97XDI6I-i8Ns16YoC59OYH6FPLO944uXMX6KCFuEJI0KMJTTS-VsdSuODGjZ69SmUYmuYb3e3PF75awrVeku_zYAwyFWmhhugQsekKD0a1XaGEATPWFF9Ia7z63PwFLGhO_-OS3KNmdpyuI8cyrHHEwF3xtIqx4d4HtwUzRFri-qkmHWyWbAykIQ6Ow-W2LVfw2eNH58C3gAhuqEPSKylOQ4kMPQOOkmaSYuByuFPh5Zi56LA3YEbPaJEuV_dOzh6d_p6TLA4UwMxNyWzEPZfaVmOBqqvuZuwBIREj1ogLajt0KqIhUG6K5pvi2Fb99KUhD3L31wv_CmpF3hzbdSlatkBSoFu601nOdk-NJWniZa6iu9EU0rFhuf2Dfbh2kHrqWYpLHvCKgHk-U5rAOWFslYlGNg; SID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLW2xS-mlmdSTxdkqK0speewACgYKAesSARQSFQHGX2MikkZ0ULg15goEz5t18SaJQhoVAUF8yKqw38t8PShDdOXclGh5Xqmd0076; __Secure-1PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsL6ZRevsd9Ur3JtMdtvR5KZQACgYKATcSARQSFQHGX2Mig7PWEJfanFzs3sU6xXAIRhoVAUF8yKpdpqwKBIJBxAVArr6POEMS0076; __Secure-3PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLkCixmOgc2FMTH30xZ4HPfgACgYKAQcSARQSFQHGX2MiKkC64r090tldq8PQdZibGhoVAUF8yKpNnBAHaospDvlKRWIcqFnm0076; HSID=Ar3yC1mBUaa1a0Fpp; SSID=AYMr1tbyU9avtPX4-; APISID=ju4fB_YNuGgBT_Pp/AOm8LL5biEbGkOgc7; SAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-1PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-3PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; SIDCC=AKEyXzWlNlMRvES5s0VflPPUK2PhYouanpKAHreZL4KtSQZPTBDG2VDgxiHpV-yGR4MTlcHfwuA; __Secure-1PSIDCC=AKEyXzWy_62-91JO99hkQHvUFvgcrDdfbZGFe8rWpIDTDrS21TTSOPnIYrxtFQEQBiOh3YjLn2k; __Secure-3PSIDCC=AKEyXzVsVKbCVdC_LksLNRUqnuI5YvruuJo-hIpXbKbfNeCXDJTJCyq5ztmg_riI5MCEq4oGsaKJ; SEARCH_SAMESITE=CgQIxp8B; __Secure-1PSIDTS=sidts-CjEBWhotCTZWm83kA9ZY2inOhquktS_lVv8PY9RzeO0rKzwjcZ9OaScvZiXdFnzJG0GGEAA; __Secure-3PSIDTS=sidts-CjEBWhotCTZWm83kA9ZY2inOhquktS_lVv8PY9RzeO0rKzwjcZ9OaScvZiXdFnzJG0GGEAA; OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYsCuqTMhL8n40u37mc4dxpwACgYKAeESARQSFQHGX2Mi1jzy-75FcNkhkBuiqj1WtBoVAUF8yKoK_-dbuCdEhcpq2O0dHC3A0076; __Secure-OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYYTQ51tXRsmSQmEs5JHUkagACgYKASQSARQSFQHGX2MiHRqlE2y_S-eTYgU0SkXnzhoVAUF8yKoXponDY9K9aaaLyKLM36DV0076; __Secure-BUCKET=CLQC; AEC=AaJma5v23HIs1lldTWaW9Jk1pb9DgsXdLIOncmDNe3ZqBTcz9mJf0jf3huE; COMPASS=appsfrontendserver=CgAQ9M-pzgYafQAJa4lXNdK39218o12B2cPkLXlou9FGt4_7JcDYZ4-HgnOHpsw9ESh_LGoS2iw4ufcm7Le-EAe9qg2jQ_FjVcOYONRpfKTJK8oZUyehGLcAy5fnou7Xgkj9ZEGzM0PLMVq2kDG3lNned2sjr2ps1QJr6JHPPX7wCEiOThdXIAEwAQ; S=billing-ui-v3=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw:billing-ui-v3-efe=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw";   public GoogleDoc? doc;

    public MainWindow()
    {
        InitializeComponent();
        UrlConfig = JsonParsing.GetUrlConfig();
        if (UrlConfig.version != urlconfig_version)
        {
            Console.WriteLine("WARNING, URL config version mismatch.");
        }
        browsercookiepaths = JsonParsing.GetBrowserCookiePaths();
        if (browsercookiepaths.version != browsercookiepath_version)
        {
            Console.WriteLine("WARNING, browser cookie paths version mismatch.");
        }
        var SaveKeys = JsonParsing.GetSaveKeys();
        if (SaveKeys.debugmenu)
        {
            Console.WriteLine("Debug menu enabled.");
            Program.DebugMenu();
        }
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
    String url = JsonParsing.GetBindReq(doc_id,UrlConfig);
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
                if (JsonParsing.TryParseFirstJsonObject(json, out JObject? obj) && obj is not null)
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
string url =  JsonParsing.InitialReq(doc_id,UrlConfig);
Console.WriteLine(url);
try
{


    string json = await NetworkManager.GetRequest(url);

  if (!JsonParsing.TryExtractFirstJsonObject(json, out JObject? parsed, out int firstStart, out int firstEnd, out string rawFirst))
  {
      MainText.Text = "Failed to parse first JSON object";
      return;
  }

  if (!JsonParsing.TryReadLengthPrefixedSegment(json, firstEnd, out string json2Raw, out int _))
  {
      MainText.Text = "Failed to parse length-prefixed second segment";
      return;
  }

// Optional sanity log (rawFirst is exact substring from server)
  Console.WriteLine($"First JSON chars: {rawFirst.Length}");
  Console.WriteLine($"Second segment chars: {json2Raw.Length}");

// If json2Raw itself contains wrappers, extract first object from it:
  if (!JsonParsing.TryExtractFirstJsonObject(json2Raw, out JObject? parsed2, out _, out _, out _))
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

}