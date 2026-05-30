using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using DryIoc.ImTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;




public partial class MainWindow : Window
{
    private static int cursorupdateinterval = (1000 / 120);
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
    private bool DebugReadSephamore = false;
    private string DebugReadText = "";
    private static SaveKeys SaveKeys;
    private readonly object debugLogLock = new();

    // Keep this in memory only; it is refreshed from the embedded login WebView.
    private string cookie = "";
    public GoogleDoc? doc;
    private string debugmenulog = "";

    public MainWindow()
    {
        InitializeComponent();
        this.AddHandler(InputElement.KeyDownEvent, MainTextKeyDown, RoutingStrategies.Tunnel);
        this.AddHandler(InputElement.KeyUpEvent, MainTextKeyUp, RoutingStrategies.Tunnel);

        Program.mainWindow = this;
        CookieManager.mainWindow = this;
        SaveKeys = JsonParsing.GetSaveKeys();
        if (SaveKeys.log)
        {
            InitLogThread();
        }
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

        if (SaveKeys.debugmenu)
        {
            Console.WriteLine("Debug menu enabled.");
            DebugMenuPopup.IsOpen = true;
            Program.DebugMenu();
        }
        else
        {
            DebugMenuPopup.IsOpen = false;
        }
        CookieManager.OvverideAlphabetical(false);
        SetMainText("Ready to go!");
        InitCursorManager();
        ActivePanel(Toolbar,false);
      //  PrintLineDebugMenu("C:\\Users\\##SYSUSER##\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Network\\Cookies".GetRealPath());
    //    ActiveElement(Toolbar, false);
       /* var items = NetworkManager.PostRequest("https://docs.google.com/v1/items:get").GetAwaiter().GetResult();
        Console.WriteLine("ITEMS: " + items);*/
        //SetCursorOffsets(50, 0);
        //Console.WriteLine(GetCookies().ToString());

    }

    public void InitLogThread()
    {
        Thread logthread = new Thread(new ThreadStart(LogThread));
        Program.CleanUp.Add(logthread);
        logthread.Start();
    }

    public void LogThread()
    {
        while (true)
        {
            string snapshot;
            lock (debugLogLock)
            {
               // debugmenulog += $"[LOG SAVED {DateTime.Now}]";
                snapshot = debugmenulog;
            }

            File.WriteAllText("./log.txt", snapshot);
            Thread.Sleep(1000);
        }
    }

    public void InitCursorManager()
    {
       /* MainText.KeyDown += (object? sender, KeyEventArgs? e) =>
        {
            switch (e.Key)
            {
                case Key.Left:
                    CursorManager.KeyDown(Move.Left);
                    break;
                case Key.Right:
                    CursorManager.KeyDown(Move.Right);
                    break;
                case Key.Up:
                    CursorManager.KeyDown(Move.Up);
                    break;
                case Key.Down:
                    CursorManager.KeyDown(Move.Down);
                    break;
                default:
                    break;
            }
        };
        MainText.KeyUp += (object? sender, KeyEventArgs? e) =>
        {
            switch (e.Key)
            {
                case Key.Left:
                    CursorManager.KeyUp(Move.Left);
                    break;
                case Key.Right:
                    CursorManager.KeyUp(Move.Right);
                    break;
                case Key.Up:
                    CursorManager.KeyUp(Move.Up);
                    break;
                case Key.Down:
                    CursorManager.KeyUp(Move.Down);
                    break;
                default:
                    break;
            }
        };*/
         MainText.LayoutUpdated += OnTextLayoutUpdated;
         CursorManager.SetTextLayout(MainText.TextLayout);
        CursorManager.Init(this);
        foreach( var line in MainText.TextLayout.TextLines)
        {
            PrintLineDebugMenu($"Line Length: {line.Length}");
        }
        Thread cursorthread = new Thread(new ThreadStart(UpdateCursorThread));
        Program.CleanUp.Add(cursorthread);
        cursorthread.Start();
    }
    public static void OnTextLayoutUpdated(object? sender, EventArgs e)
    {
        Program.mainWindow.PrintLineDebugMenu("\n Text layout updated");
        var mainText = Program.mainWindow.GetMainText();
        var textlayout = mainText.TextLayout;
        CursorManager.SetTextLayout(textlayout);
    }

    public static void UpdateCursorThread()
    {
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            CursorManager.UpdateCursor();
            CursorManager.UpdateCursorPosition();
            stopwatch.Stop();
            CursorManager.PrintLineDebugMenu($"Cursor update took {stopwatch.ElapsedTicks * 100} ns");
            int sleepTime = cursorupdateinterval - (int)stopwatch.ElapsedMilliseconds;
            Thread.Sleep(Math.Max(0, sleepTime));
        }
    }

    public TextBlock GetMainText()
    {
        return MainText;
    }

    public void SetCursorOffsets(float x, float y)
    {
        var margin = new Thickness(x, y, 0, 0);
      Cursor.Margin = margin;
    }
    private void SetMainText(string text,bool recurs = false)
    {
       
       // Console.WriteLine("Setting main text: " + text + " " + recurs);
        if(!recurs)
        {
            MainText.Text = "";
        MainText.Inlines.Clear();
        }
        string[] inlines = new[] {"<Bl/>", "</Bl>", "<It/>", "</It>", "<Tb/>", "</Tb>"};
        bool ctns = false;
        int index = int.MaxValue;
        for (int i = 0; i < inlines.Length; i++)
        {
            ctns = ctns || text.Contains(inlines[i]);
            if(ctns)
            {
                int tmpindex = text.IndexOf(inlines[i], StringComparison.Ordinal);
                // Console.WriteLine("Found inline " + inlines[i] + " at index " + tmpindex);
                if(tmpindex != -1)
                {
                index = Math.Min(index, tmpindex);
                }
            }
        }
        if (ctns)
        {
            //Console.WriteLine("Adding plain text inline: " + text.Substring(0, index));
            MainText.Inlines.Add(new Run(text.Substring(0, index)));
             // MainText.Inlines.Add(new Run("1237656544444"));
            string remaining = text.Substring(index);
            string after = "";
            if(remaining.StartsWith("<Bl/>"))
            {
                string bld = remaining.Substring(5, remaining.IndexOf("</Bl>", StringComparison.Ordinal) - 5);
               //Console.WriteLine("Adding bold text inline: " + bld);
                var bold = new Bold();
                bold.Inlines.Add(new Run(bld));
                MainText.Inlines.Add(bold);
                after = remaining.Substring(5 + bld.Length + 5);
                //Console.WriteLine("Remaining text: " + after);
            }
            else if(remaining.StartsWith("<It/>"))
            {
               string itl = remaining.Substring(5, remaining.IndexOf("</It>", StringComparison.Ordinal) - 5);
                Console.WriteLine("Adding italic text inline: " + itl);
                var italic = new Italic();
                italic.Inlines.Add(new Run(itl));
                MainText.Inlines.Add(italic);
                after = remaining.Substring(5 + itl.Length + 5);
               // Console.WriteLine("Remaining text: " + after); 
            }
            else if(remaining.StartsWith("<Tb/>"))
            {
               // Console.WriteLine("Adding table inline");
                string tbl = remaining.Substring(5, remaining.IndexOf("</Tb>", StringComparison.Ordinal) - 5);
                int height = Regex.Count(tbl,"\u0012");
                int total = Regex.Count(tbl,"\u001c");
                int width = total / height;
                //Console.WriteLine("Adding table inline with width " + width + " and height " + height);
                string[,] table = new string[width,height];
              //  Console.WriteLine("Adding table text inline: " + tbl);
                var tablecon = new InlineUIContainer();
                var grid = new Grid();
                grid.ColumnDefinitions = new ColumnDefinitions();
                grid.RowDefinitions = new RowDefinitions();
                //TODO: make lines appear in between cells
                int i = 0;
                foreach(string cell in tbl.Split("\u001c").RemoveAt(0))
                {
                    int col = i % width;
                    int row = i / width;
                    if(col == 0)
                    {
                        grid.RowDefinitions.Add(new RowDefinition());
                    }
                    if(row == 0)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition());
                    }
                    var textblock = new TextBlock();
                    string clean = cell.Replace("\u0012","");
                    textblock.Text = clean;
                    Grid.SetColumn(textblock,col);
                    Grid.SetRow(textblock,row);
                    grid.Children.Add(textblock);
                    i++;
                }
                tablecon.Child = grid;
                MainText.Inlines.Add(tablecon);
                after = remaining.Substring(5 + tbl.Length + 5);
               // Console.WriteLine("Remaining text: " + after);
            }
            if(!string.IsNullOrEmpty(after.Trim()))
            {
            SetMainText(after,true);
            }
        }
        else
        {
            MainText.Inlines.Add(new Run(text));
        }
        
          /* foreach(var inline in MainText.Inlines)
        {
           Console.WriteLine(inline);
            if(inline is Run run)
            {
                Console.WriteLine("Run text: " + run.Text);
             //   run.Text = run.Text.Replace("\\n","\n");
            }
            else
            {
                Console.WriteLine("Not run");
            }
        }*/
        
    }



    private string CharToString(char c)
    {
        return c.ToString();
    }
    private void SubmitCookiePath(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CookiePathInput.Text))
        {
            return;
        }
       CookieManager.ManualInputCallback(CookiePathInput.Text.Trim());
        ManualCookiePopup.IsOpen = false;
    }
    private void CloseDebugMenu(object? sender, RoutedEventArgs e)
    {
        DebugMenuPopup.IsOpen = false;
    }
    public void SetOpenManualInput(bool val)
    {
        ManualCookiePopup.IsOpen = val;

    }
    public void SetOpenPickBrowser(bool val)
    {
        PickCookiePopup.IsOpen = val;
    }
    public void SetPickOptions(List<string> options)
    {
      //  Console.WriteLine($"SetPickOptions called. options={options.Count}, existingChildren={BrowserList.Children.Count}");
        if (BrowserList.Children.Count > 1)
        {
            while (BrowserList.Children.Count > 1)
            {
                BrowserList.Children.RemoveAt(1);
            }
        }

        foreach (string option in options)
        {
            Button button = new Button
            {
                Content = option,
                Margin = new Thickness(5),
                Width = 100,
                Height = 50
            };
            button.Click += (sender, e) =>
            {
                CookieManager.PickBrowserCallback(option);
                PickCookiePopup.IsOpen = false;
            };
            Console.WriteLine("Adding browser option: " + option);
            BrowserList.Children.Add(button);
        }
      //  Console.WriteLine($"BrowserList now has {BrowserList.Children.Count} children.");
    }

    public void OpenDebugMenu(object? sender, RoutedEventArgs e)
    {
        DebugMenuPopup.IsOpen = true;
    }
    public void FeelingLucky(object? sender, RoutedEventArgs e)
    {
        var tmpsavekeys = JsonParsing.GetSaveKeys();
        OpenDoc(tmpsavekeys.lastopened);
    }

















    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        doc_id = docidbox.Text;
    }

    private Stream BindRequest(String url)
    {
        string sid = NetworkManager.GetSid();
        if (!string.IsNullOrEmpty(sid))
        {
            url += $"&sid={sid}";
            Console.WriteLine($"Updated bind URL with sid: {url}");
        }
        return client.GetStreamAsync(url).Result;
    }

    private async void BindToDoc()
    {
    String url = JsonParsing.GetBindReq(doc_id,UrlConfig);
    url += $"&zx={new Random().Next(100000,999999)}";
    url += $"&RID={new Random().Next(100000,999999)}";
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
        SetMainText(doc.GetText());
        Console.WriteLine(json);
    }
    }
    private void OpenDocButtonCallback(object? sender, RoutedEventArgs e)
    {
        OpenDoc();
    }
    private async void OpenDoc(string docid = "")
    {
SetMainText("Loading...");
Console.WriteLine("Loading...");
string url = "";
if (string.IsNullOrEmpty(docid))
{
    url = JsonParsing.InitialReq(doc_id, UrlConfig);
}
else
{
    doc_id = docid;
    url = JsonParsing.InitialReq(docid, UrlConfig);
}
Console.WriteLine(url);
try
{


    string json = await NetworkManager.GetRequest(url);
    Console.WriteLine(json);
  if (!JsonParsing.TryExtractFirstJsonObject(json, out JObject? parsed, out int firstStart, out int firstEnd, out string rawFirst))
  {
      SetMainText("Failed to parse first JSON object");
      return;
  }

  if (!JsonParsing.TryReadLengthPrefixedSegment(json, firstEnd, out string json2Raw, out int _))
  {
     SetMainText("Failed to parse length-prefixed second segment");
      return;
  }

// Optional sanity log (rawFirst is exact substring from server)
  Console.WriteLine($"First JSON chars: {rawFirst.Length}");
  Console.WriteLine($"Second segment chars: {json2Raw.Length}");

// If json2Raw itself contains wrappers, extract first object from it:
  if (!JsonParsing.TryExtractFirstJsonObject(json2Raw, out JObject? parsed2, out _, out _, out _))
  {
      SetMainText("Failed to parse second JSON object");
      return;
  }

  doc = new GoogleDoc(parsed!, parsed2!);
  doc.id = doc_id;
  await doc.GetSessionId();
  SetMainText(doc.GetText());
  Console.WriteLine("Document loaded successfully.");
  Console.WriteLine(doc.GetText());
  if(SaveKeys.bind)
  {
      BindToDoc();
  }
  ActiveElement(FeelingLuckyButton,false);
  ActiveElement(OpenDebugMenuButton,false);
  ActiveElement(OpenDocButton,false);
  ActiveElement(docidbox,false);
    ActivePanel(Toolbar,true);
 // MainText.Inlines.Add(new Run("Hello World"));

}
catch (HttpRequestException err)
{
    Console.WriteLine(err.Message);
    SetMainText(err.Message);
}
    }

    public void PrintDebugMenu(string s)
    {
        if (SaveKeys.log)
        {
        DebugMenuTextBlock.Text += s;
            lock (debugLogLock)
            {
                debugmenulog += s;
            }
        }
    }

    public void ActiveElement(InputElement element, bool active)
    {
        element.IsEnabled = false;
        element.IsVisible = false;
    }

    public void ActivePanel(Panel panel, bool active)
    {
        panel.IsEnabled = active;
        panel.IsVisible = active;
    }

    public void PrintLineDebugMenu(string s)
    {
        if (SaveKeys.log)
        {
            DebugMenuTextBlock.Text += s + "\n";
            lock (debugLogLock)
            {
                debugmenulog += s + "\n";
            }
        }
    }

    private void SendDebugMenuText(object? sender, RoutedEventArgs e)
    {
        DebugReadText = DebugMenuInput.Text;
    DebugReadSephamore = true;
    }

    public async Task<string> ReadDebugMenu()
    {
                while (true)
                {
                    if (DebugReadSephamore)
                    {
                        DebugReadSephamore = false;
                        return DebugReadText;
                    }
                    await Task.Delay(100);
                   // Console.WriteLine(DebugReadText);
                }
    }

    private void MainTextKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                CursorManager.KeyDown(Move.Left);
                break;
            case Key.Right:
                CursorManager.KeyDown(Move.Right);
                break;
            case Key.Up:
                CursorManager.KeyDown(Move.Up);
                break;
            case Key.Down:
                CursorManager.KeyDown(Move.Down);
                break;
            default:
                bool isAlphabet = e.Key >= Key.A && e.Key <= Key.Z;
                Edit? edit = null;
                var pos = CursorManager.GetCursorPosition();
                if(isAlphabet)
                {
                    int letter = e.Key - Key.A + 1;
                    char c = (char)('a' + letter - 1);
                    string s = CharToString(c);
                    edit = new Edit(EditType.Insert,
                        new string[] { pos.ToString(),s});
                    Program.mainWindow.PrintLineDebugMenu($"Inserting {s} at {pos}");
                }

                switch (e.Key)
                {
                    case Key.Enter:
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(),"\n"});
                        break;
                    case Key.Tab:
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(),"\u0009"});
                        break;
                }

                if (edit != null)
                {
                    doc.history.Edits.Add(edit);
                    SetMainText(doc.GetText());
                    CursorManager.MoveCursor();
                    doc.OffsetAltersAfter(1, pos);
                }

                break;
        }
    }

    private void MainTextKeyUp(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                CursorManager.KeyUp(Move.Left);
                break;
            case Key.Right:
                CursorManager.KeyUp(Move.Right);
                break;
            case Key.Up:
                CursorManager.KeyUp(Move.Up);
                break;
            case Key.Down:
                CursorManager.KeyUp(Move.Down);
                break;
            default:
                break;
        }
    }
}