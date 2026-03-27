using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace GoogleDocs;

public partial class MainWindow : Window
{
    private static String currentUrl = "";
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private string doc_id = "";

    private string cookie =
        "COMPASS=documents=CmIACWuJV_IWtEZzP8wzCogz5MKscNE7tNzIbc_nFv6KOLDixhsDadJbZ-Kq4RZXrUHZihpvXT6KVwCeUmh9LQJvU1z0SG4g-efIB-rqQ1Aeu2ATrWOLgWfU6NQePYumZPJ_bRDr5ZnOBhqEAQAJa4lXwuVFmeJbxy88F2IPzF3ZLPUHVx0tUkL-jiSJxis1zBnLO2gHuNQ6lQfWW_ctw_ilX6gEoqNMjuMDztONVs-wGM_nn2BbytXKasb41AhLAn1dSRXVzFyuMT2QhOWu7BCn9rQcefJJPI7Hwy5vYZgwhlxSBamxyi1-e2zjwTGvGQ==; NID=530=m4p59CNbB_Q9KuGqtE_XwhEoeSCNZj9kFmHrf4tUOjGkpvnIAVi4U19oMIOe27BjJ0AxLBsrTfy-OmsHlw9qCqnlKvQJhJkLvvXf6zP4yeTHmgDdRrO-vTNfobomYRQ_ABtqED5O5dYg9P3nYsNNLEKSmlv0syOGQLnP6Wu7xTk0OZBdhHVW2FLtZLyph-K6gxuDw20t8QWo4PXwOtQn-ouuqFaeqIsZxnYoNpWn0Oc48TFSY6b6nfFkJUElBOIsaQoyiXWxK0SVtJv5UeM7_CHmxTxDEL8C2pJJOtsqmd_9iKXhhcdTsEKV1UsOeUxHcYBTdDYbXaY4L5YQ0QHRdnqbHOPgvfNvMUX1pE3tA3DOCTOm3aePJaRZD5naylc5W2g_3r0zGonwiS2iSVQh-hrs_1uhxkUvjRXb1eSMXC-fFvuysLnubRVGHdwgkieclb-ko6KMUEou5-xvOr8AoR9sLRurTRWD3h23mCf6gi3Ji8scSL3h00bJZHDSSrmO7lneKK5n7GSl3WSBs5Qc1wqanV4fsZD3ItM_0g3ry1rXAdvGD-xpWMvzlKqgWEFlFolShHLDRrDmuB_CJlrenVwEB8cQhT1GkmvUxHIU8wbM2BLRbxKjQIf5yucMFYerDG_IxuaT21Q2cdxtl5Twy6AlbOzoUPb025T6nQviYiuyE5ARDs7UfHQkqa7rEJH7zUClTiYFQN4K6Adk11WmN34iTfiZw3fMRguJEtPHM1W71jmJWapUQp3Wk85bthtDdvrSwdBFpdMYmjinmYSGpaLr_Q-MQ0VxZ3y1rwx5qAE84PE; SID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLW2xS-mlmdSTxdkqK0speewACgYKAesSARQSFQHGX2MikkZ0ULg15goEz5t18SaJQhoVAUF8yKqw38t8PShDdOXclGh5Xqmd0076; __Secure-1PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsL6ZRevsd9Ur3JtMdtvR5KZQACgYKATcSARQSFQHGX2Mig7PWEJfanFzs3sU6xXAIRhoVAUF8yKpdpqwKBIJBxAVArr6POEMS0076; __Secure-3PSID=g.a0007whah4bEqXeFBsUe4RPuTfjxsah0lJmnVRH9DgReqWQDoWsLkCixmOgc2FMTH30xZ4HPfgACgYKAQcSARQSFQHGX2MiKkC64r090tldq8PQdZibGhoVAUF8yKpNnBAHaospDvlKRWIcqFnm0076; HSID=Ar3yC1mBUaa1a0Fpp; SSID=AYMr1tbyU9avtPX4-; APISID=ju4fB_YNuGgBT_Pp/AOm8LL5biEbGkOgc7; SAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-1PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; __Secure-3PAPISID=9MbuET2lqZp5FZ74/A1OrLj5oCIutPIflT; SIDCC=AKEyXzVm9jx8ouHmQ78KQ97P5wFii125Lm4BqxlE1pSJb5CKuDZJyxbA2p_vUhHxgL2RoDctNXY; __Secure-1PSIDCC=AKEyXzW6cr8Vx6m_m1tCFLM10V6tnLURH2tF2gIcYdpF6_ob774awvtvN3WSMjbiLOERvpmSwLw; __Secure-3PSIDCC=AKEyXzVBtzEzhCn3diCm8_RnqawyHR5Qxqu2bpAMLj7bvNyLbN_Nb5gbYxFRzPOqb6ki8FG4xzR9; SEARCH_SAMESITE=CgQIxp8B; __Secure-1PSIDTS=sidts-CjEBWhotCV2JDuTEX9ffeVFuXEptl-S2CpxZfLqXvMIdr8dWacNf1Qpt2Ij3ph2YdxhCEAA; __Secure-3PSIDTS=sidts-CjEBWhotCV2JDuTEX9ffeVFuXEptl-S2CpxZfLqXvMIdr8dWacNf1Qpt2Ij3ph2YdxhCEAA; OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYsCuqTMhL8n40u37mc4dxpwACgYKAeESARQSFQHGX2Mi1jzy-75FcNkhkBuiqj1WtBoVAUF8yKoK_-dbuCdEhcpq2O0dHC3A0076; __Secure-OSID=g.a0007whah5DbtxM3FlZi5AhyI6b5UlO5X0JDSVuos302vmQnljrYYTQ51tXRsmSQmEs5JHUkagACgYKASQSARQSFQHGX2MiHRqlE2y_S-eTYgU0SkXnzhoVAUF8yKoXponDY9K9aaaLyKLM36DV0076; __Secure-BUCKET=CLQC; AEC=AaJma5vT-RdpcwP7j7D8TJHEo7IV8YQFnEl5X12P5DZCq3V7T96Ohbap9Vw; COMPASS=appsfrontendserver=CgAQ6uWZzgYafQAJa4lXNdK39218o12B2cPkLXlou9FGt4_7JcDYZ4-HgnOHpsw9ESh_LGoS2iw4ufcm7Le-EAe9qg2jQ_FjVcOYONRpfKTJK8oZUyehGLcAy5fnou7Xgkj9ZEGzM0PLMVq2kDG3lNned2sjr2ps1QJr6JHPPX7wCEiOThdXIAEwAQ; S=billing-ui-v3=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw:billing-ui-v3-efe=fBvsBAkD9DHgOVj-4CaHL5ZCgXmzFNiQ4fTQFoqjMPw";
    public MainWindow()
    {
        InitializeComponent();
      /*  LoginPage.NavigationCompleted += LoginPage_OnNavigationCompleted;
        LoginPage.Url = new Uri("https://accounts.google.com/ServiceLogin");*/
    }

    private void LoginPage_OnNavigationCompleted(object? sender, EventArgs e)
    {
        string url = "";

        var eventType = e.GetType();

        var prop = eventType.GetProperty("Url")
                   ?? eventType.GetProperty("Uri")
                   ?? eventType.GetProperty("Address");

        var value = prop?.GetValue(e);
        if (value is Uri uri) url = uri.ToString();
        else if (value is string s) url = s;

      /*  if (string.IsNullOrWhiteSpace(url))
            url = LoginPage.Url?.ToString() ?? "";*/
        currentUrl = url;
        // Read from the control, not event args var currentUrl = LoginPage.Url?.ToString() ?? string.Empty;
        Console.WriteLine($"Navigated: {currentUrl}");

        if (currentUrl.Contains("myaccount.google.com", StringComparison.OrdinalIgnoreCase))
        {
            Dispatcher.UIThread.Post(() =>
            {
                LoginPage.Url = new Uri("https://docs.google.com/document/u/0");
            });
        }
    }


    private String Cookies()
    {
        return "";
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        doc_id = docidbox.Text;
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
    if (json.Contains("[{\"ty\":\"is\",\"ibi\":1,\"s\":\""))
    {
        string last = json.Split("[{\"ty\":\"is\",\"ibi\":1,\"s\":\"")[1];
        string doc = last.Split("\"")[1];
        MainText.Text = doc;
        Console.WriteLine(doc);
    }
    else
    {
        MainText.Text = "JSON is invalid";
        Console.WriteLine("JSON is invalid");
    }
}
catch (HttpRequestException err)
{
    Console.WriteLine(err.Message);
    MainText.Text = err.Message;
}
    }

    private async Task<string> GetRequest(string url)
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
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");

        using var response = await localClient.SendAsync(request);

        Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

        if (response.Headers.Location is not null)
            Console.WriteLine($"Redirect to: {response.Headers.Location}");

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body.Length > 500 ? body[..500] : body);

        response.EnsureSuccessStatusCode();
        return body;
    }

    private String InitialReq(String docid)
    {
        return $"https://docs.google.com/document/d/{docid}/mobile/edit?reason=2&sid=20e312e79cf3bc0e&smv=2147483647&mmv=1&smb=%5B2147483647%2C%20oAMQ%5D&fcs=1710";
    }
}