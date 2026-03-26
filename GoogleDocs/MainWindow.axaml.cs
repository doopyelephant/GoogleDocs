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
    private static readonly HttpClient client = new HttpClient();
    private string doc_id = "";

    public MainWindow()
    {
        InitializeComponent();
        LoginPage.NavigationCompleted += LoginPage_OnNavigationCompleted;
        LoginPage.Url = new Uri("https://accounts.google.com/ServiceLogin");
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

    private void OpenDoc(object? sender, RoutedEventArgs e)
    {
Main.Text = "Loading...";
string url =  InitialReq(doc_id);
Console.WriteLine(url);
try
{
    string json = GetRequest(url).Result;
    if (json.Contains("[{\"ty\":\"is\",\"ibi\":1,\"s\":\""))
    {
        string last = json.Split("[{\"ty\":\"is\",\"ibi\":1,\"s\":\"")[1];
        string doc = last.Split("\"")[1];
        Main.Text = doc;
    }
    else
    {
        Main.Text = "JSON is invalid";
    }
}
catch (HttpRequestException err)
{
    Main.Text = err.Message;
}
    }

    private Task<String> GetRequest(string url)
    {
        return client.GetStringAsync(url);
    }

    private String InitialReq(String docid)
    {
        return $"https://docs.google.com/document/d/{docid}/mobile/edit?reason=2&sid=20e312e79cf3bc0e&smv=2147483647&mmv=1&smb=%5B2147483647%2C%20oAMQ%5D&fcs=1710";
    }
}