using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    private bool ctrl = false;
    private bool shift = false;
    public List<(int, int)> Selections;
    private List<(int, int)> PrevSelections;
    private List<Rectangle> SelectionRects;


    private string cookie = "";
    public GoogleDoc? doc = null;
    private string debugmenulog = "";
    private int rid = 0;


    public MainWindow()
    {
        InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, MainTextKeyDown, RoutingStrategies.Tunnel);
            this.AddHandler(InputElement.KeyUpEvent, MainTextKeyUp, RoutingStrategies.Tunnel);
            MainText.AddHandler(InputElement.PointerPressedEvent, MainTextMouseDown, RoutingStrategies.Tunnel);
            Program.mainWindow = this;
            CookieManager.mainWindow = this;
            NetworkManager.loggingdest = this;
            CursorManager.Init(this);
            GoogleDoc.loggingdest = this;
            SaveKeys = JsonParsing.GetSaveKeys();
            if (SaveKeys.log)
            {
                InitLogThread();
            }

            if (!SaveKeys.hassetup)
            {
                SaveKeys.acceptedbrowserscraping = false;
                JsonParsing.SaveKeys(SaveKeys);
                PermissionPrompt.Open();
            }
            else
            {
                if (!SaveKeys.acceptedbrowserscraping)
                {
                    PermBox.Text =
                        "You have not accepted browser scraping, this is required for this application to function. \n\n" +
                        PermBox.Text;
                    PermissionPrompt.Open();
                }
            }


            UrlConfig = JsonParsing.GetUrlConfig();
            if (UrlConfig.version != urlconfig_version)
            {
                PrintLineDebugMenu("WARNING, URL config version mismatch.");
            }

            browsercookiepaths = JsonParsing.GetBrowserCookiePaths();
            if (browsercookiepaths.version != browsercookiepath_version)
            {
                PrintLineDebugMenu("WARNING, browser cookie paths version mismatch.");
            }

            if (SaveKeys.debugmenu)
            {
                PrintLineDebugMenu("Debug menu enabled.");
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
            ActivePanel(Toolbar, false);
            ActiveElement(FeelingLuckyButton, SaveKeys.lastopened != "");
            OpenDebugMenuButton.IsVisible = SaveKeys.debugmenu;
            //  PrintLineDebugMenu("C:\\Users\\##SYSUSER##\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Network\\Cookies".GetRealPath());
            //    ActiveElement(Toolbar, false);
            /* var items = NetworkManager.PostRequest("https://docs.google.com/v1/items:get").GetAwaiter().GetResult();
             PrintLineDebugMenu("ITEMS: " + items);*/
            //SetCursorOffsets(50, 0);
            //PrintLineDebugMenu(GetCookies().ToString());
            if (SaveKeys.acceptedbrowserscraping)
            {
                CookieManager.InitCookies(SaveKeys);
            }

            StartSaveThread();

    }

    private void MainTextMouseDown(object? sender, PointerPressedEventArgs e)
    {
        lock (CursorManager.CursorLock)
        {
            var hittest = MainText.TextLayout.HitTestPoint(e.GetPosition(MainText));
            CursorManager.Position.X = hittest.TextPosition;
            CursorManager.Position.Y = 0;
            CursorManager.LastCursorPosition = CursorManager.Position;
            var offset = MainText.TextLayout.HitTestTextPosition(hittest.TextPosition);
            CursorManager.LastCursorOffset = new Vector2((float)offset.X, (float)offset.Y);
            CursorManager.changedflag = true;
        }
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
        lock (CursorManager.CursorLock)
        {
            Program.mainWindow.PrintLineDebugMenu("\n Text layout updated");
            var mainText = Program.mainWindow.GetMainText();
            var textlayout = mainText.TextLayout;
            CursorManager.SetTextLayout(textlayout);
        }
    }

    public static void UpdateCursorThread()
    {
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            CursorManager.UpdateCursor();
            CursorManager.UpdateCursorPosition();
            stopwatch.Stop();
            CursorManager.PrintLineDebugMenu($"Cursor update took {(stopwatch.ElapsedTicks * 100f)/1000000f} ms");
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
    private void SetMainText(string text,List<RichText>? richTexts = null)
    {
        // Start Table \u0010
        //End Table \u0011
       var watch = new Stopwatch();
       watch.Start();
       MainText.Text = "";
       MainText.Inlines = new InlineCollection();
       if (richTexts == null)
       {
           MainText.Text = text;
       }
       else
       {
           var masterstages = new List<(int, int, List<RichText>)>();
           richTexts.Sort((x, y) => { return x.start - y.start; });
           for (int j = 0; j < richTexts.Count; j++)
           {
               int jtmp = j;
               List<RichText> alike = new List<RichText>();
               alike.Add(richTexts[j]);
               List<(int, int, List<RichText>)> stages = new List<(int, int, List<RichText>)>();
               while (j < richTexts.Count - 1 && richTexts[j].start == richTexts[j + 1].start)
               {
                   j++;
                   alike.Add(richTexts[j]);
               }

               if (alike.Count > 1)
               {
                   if (alike.Select(x => x.end).Distinct().Count() == 1)
                   {
                    stages.Add((richTexts[j].start, richTexts[j].end, alike.ToList()));
                   }
                   else
                   {
                       alike.Sort((x, y) => { return x.end - y.end; });
                       int curend = alike[0].end;
                       List<int> tmpindices = new List<int>();
                       for (int k = 1; k < alike.Count; k++)
                       {
                           if (alike[k].end != curend)
                           {
                               curend = alike[k].end;
                               stages.Add((alike[k - 1].start, richTexts[k - 1].end, tmpindices.Select(x => alike[x]).ToList()));
                               tmpindices.Clear();
                           }
                           else
                           {
                               tmpindices.Add(k);
                           }
                       }
                   }
               }
               else
               {
                   stages.Add((richTexts[j].start, richTexts[j].end, new [] {richTexts[j]}.ToList()));
               }

               foreach (var stage in stages)
               {
                   masterstages.Add(stage);
               }
           }
           int inserted = 0;
           foreach (var stage in masterstages)
           {
               var start = stage.Item1;
               var end = stage.Item2;
               var texts = stage.Item3;
               if (inserted < start)
               {
                   MainText.Inlines.Add(new Run(text.Substring(inserted, start - inserted)));
               }
                var inline = new Run(text.Substring(start, end - start));
                foreach (var t in texts)
                {
                    switch (t.Type)
                    {
                        case RichTextType.Bold:
                            inline.FontWeight = FontWeight.Bold;
                            break;
                        case RichTextType.Italic:
                            inline.FontStyle = FontStyle.Italic;
                            break;
                    }
                }
                MainText.Inlines.Add(inline);
                inserted = end;
           }

           if (inserted < text.Length)
           {
               MainText.Inlines.Add(new Run(text.Substring(inserted, text.Length - inserted)));
           }
       }

              //  string tbl = ""/* Table Text*/;
              /*  int height = Regex.Count(tbl,"\u0012");
                int total = Regex.Count(tbl,'\u001c'.ToString());
                int width = total / height;
                //PrintLineDebugMenu("Adding table inline with width " + width + " and height " + height);
                string[,] table = new string[width,height];
              //  PrintLineDebugMenu("Adding table text inline: " + tbl);
                var tablecon = new InlineUIContainer();
                var grid = new Grid();
                grid.ColumnDefinitions = new ColumnDefinitions();
                grid.RowDefinitions = new RowDefinitions();
                //TODO: make lines appear in between cells
                int i = 0;
                var column = tbl.Split("\u001c").ToList();
                column.RemoveAt(0);
                foreach(string cell in column)
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
                MainText.Inlines.Add(tablecon);*/
                UpdateSelections();
                watch.Stop();

                PrintLineDebugMenu($"Set main text in {watch.ElapsedMilliseconds} ms");

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

    public void SetPromptOptions(List<String> options)
    {
        if (PromptList.Children.Count > 1)
        {
            while (PromptList.Children.Count > 1)
            {
                PromptList.Children.RemoveAt(1);
            }
        }

        foreach (string option in options)
        {
            Button button = new Button
            {
                Content = option,
                Margin = new Thickness(5),
                Width = 300,
                Height = 50
            };
            button.Click += (sender, e) =>
            {
                CookieManager.PromptCallback(option);
            };
            PromptList.Children.Add(button);
        }
    }
    public void SetPickOptions(List<string> options)
    {
        PrintLineDebugMenu($"SetPickOptions called. options={options.Count}, existingChildren={BrowserList.Children.Count}");
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
            PrintLineDebugMenu("Adding browser option: " + option);
            BrowserList.Children.Add(button);
        }
        PrintLineDebugMenu($"BrowserList now has {BrowserList.Children.Count} children.");
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



    public void SaveThread()
    {
        while (true)
        {
            if (doc != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { doc.Save(); });
            }

            Thread.Sleep(1000);
        }
    }

    public void StartSaveThread()
    {
        var thread = new Thread(SaveThread);
        thread.Start();
    }













    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        doc_id = docidbox.Text;
    }

    private async IAsyncEnumerable<string> BindRequest(String url, [EnumeratorCancellation] CancellationToken cancel)
    {
        string sid = NetworkManager.GetSid();
        if (!string.IsNullOrEmpty(sid))
        {
            url += $"&sid={sid}";
            PrintLineDebugMenu($"Updated bind URL with sid: {url}");
        }

        await foreach (var block in NetworkManager.GetStreamAsync(url, cancel, true))
        {
            yield return block;
        };
    }

    private async Task<string> BindToDoc(string extra = "",bool isexternalthread = false)
    {
    String url = JsonParsing.GetBindReq(doc_id,UrlConfig);
  /*  url += $"&zx={new Random().Next(100000,999999)}";
    url += $"&RID={rid++}";*/
    url += extra;

    PrintLineDebugMenu(url);
    string tmpaid = "";
  /*  while (true)
    {
        PrintLineDebugMenu("Binding...");
        Stream jsonstream = await BindRequest(url);
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
        string json = new string(buffer);*/
  await foreach (var strjson in BindRequest(url, CancellationToken.None))
  {
      JsonParsing.TryParseFirstJsonObject(strjson, out var fjson);
     /* foreach (var json in fjson[0][1][2]["c"][])
      {*/
          PrintLineDebugMenu("JSON: " + fjson.ToString(Formatting.Indented));
          foreach (var innerjson in fjson.Select(x =>
                   {
                       if (x.Count() < 1)
                       {
                           PrintLineDebugMenu("noop");
                           return new JArray();
                       }
                       var p = x[1];
                       if (p.Count() < 2)
                       {
                           PrintLineDebugMenu("noop");
                           return new JArray();
                       }
                       if (p[2] is JObject && p[2]["c"] is JArray)
                       {
                           PrintLineDebugMenu("AID: " + x[0].ToString());
                           tmpaid = x[0].ToString();
                           return p[2]["c"];
                       }
                       PrintLineDebugMenu("noop");
                       return new JArray();
                   }).ToList())
          {
              foreach (var json in innerjson.Select(x => x[0]))
              {
                  if (json.Parent[4].Type == JTokenType.String && SaveKeys.verbose)
                  {
                      if (!File.Exists("siddebug.txt"))
                      {
                          File.Create("siddebug.txt").Dispose();
                      }
                      File.WriteAllText("siddebug.txt",File.ReadAllText("siddebug.txt") + ", " + json.Parent[4].ToString());
                  }
                  if (json.Parent[4].Type == JTokenType.String && json.Parent[4].ToString() == NetworkManager.sid)
                  {

                      PrintLineDebugMenu("sid matched, loopback detected, skipping");
                    continue;
                  }
                  if (json["ty"].ToString() == "noop")
                  {
                      doc.history.Edits.Add(new Edit(EditType.Noop, new string[0],true));
                      continue;
                  }
                    PrintLineDebugMenu("TYPE : " + json["ty"].ToString());
                  if (json["ty"].ToString() == "as" && json["st"].ToString() == "spellcheck")
                  {
                      continue;
                  }
                  var edit = new Edit(json as JObject,true);
                  doc.history.Edits.Add(edit);
                  if (edit.Type == EditType.Insert)
                  {
                      PrintLineDebugMenu("Offseting..");
                      doc.OffsetAltersAfter(edit.Params[1].Length,int.Parse(edit.Params[0]));
                  }
                  if (edit.Type == EditType.Delete)
                  {
                      PrintLineDebugMenu("Negative Offseting..");
                      doc.OffsetAltersAfter(int.Parse(edit.Params[1]) - int.Parse(edit.Params[0]),int.Parse(edit.Params[1]));
                  }


                  if (isexternalthread)
                  {
                      var text = doc.GetText();
                      Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => SetMainText(text.Item1,text.Item2));
                  }
                  else
                  {
                      var text = doc.GetText();
                      SetMainText(text.Item1,text.Item2);
                  }

                  PrintLineDebugMenu(json.ToString());
              }
          }
          //  }
  }

  return tmpaid;
  // }
    }
    private void OpenDocButtonCallback(object? sender, RoutedEventArgs e)
    {
        OpenDoc();
    }
    public async void OpenDoc(string docid = "")
    {
        if (SaveKeys.verbose)
        {
          /*  var homepage = await NetworkManager.GetRequest("https://docs.google.com/document/u/0",true,new [] {("Sec-Fetch-Dest","document"),("Priority","u=0, i"),("Sec-Fetch-Mode","navigate"),("Sec-Fetch-Site","none"),("Sec-Fetch-User","?1"),("Sec-GPC","1"),("Upgrade-Insecure-Requests","1")}.ToList());
            File.WriteAllText("homepage.txt",homepage);
            var key = homepage.SubstringAfter("client_key\":\"").SubstringBefore("\"");//* "AIzaSyDl-UL2oekTnhhyaKOSEIX2fYcWIapfhR0";
            File.WriteAllText("homekey.txt",key);
            File.WriteAllText("items.txt",
                await NetworkManager.PostRequest($"https://drivefrontend-pa.clients6.google.com/v1/changes:list?key={key}",
                    "[[null,1,null,null,1],[33501,1000]]",true,new [] {("authorization","SAPISIDHASH 1789332404_13ea55c355d34ac42a6d3414dedd6bc98a009a93_u SAPISID1PHASH 1789332404_13ea55c355d34ac42a6d3414dedd6bc98a009a93_u SAPISID3PHASH 1789332404_13ea55c355d34ac42a6d3414dedd6bc98a009a93_u")}.ToList()));
      */ }
        var watch = new Stopwatch();
        watch.Start();
SetMainText("Loading...");
PrintLineDebugMenu("Loading...");
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
PrintLineDebugMenu(url);
try
{


    string json = await NetworkManager.GetRequest(url);
    PrintLineDebugMenu(json);
    int firstEnd = 0;
  if (!JsonParsing.TryExtractFirstJsonObject(json, out JObject? parsed, out int firstStart, out firstEnd, out string rawFirst))
  {
      SetMainText("Failed to parse first JSON object");
      return;
  }

  List<JObject> jsons = new List<JObject>();

  while (JsonParsing.TryReadLengthPrefixedSegment(json, firstEnd, out string json2, out firstEnd))
  {
      if (!JsonParsing.TryExtractFirstJsonObject(json2, out JObject? parsed2, out _, out _, out _))
      {

          if (jsons.Count == 0)
          {
              SetMainText("Failed to parse second JSON object");
              return;
          }
          else
          {
              PrintLineDebugMenu("Failed to parse second JSON object");

              break;
          }
      }
      else
      {
          PrintLineDebugMenu("Adding Jsons");
          jsons.Add(parsed2);
      }
  }
  PrintLineDebugMenu($"Jsons Finished: {jsons.Count} ");
  foreach (var x in jsons)
  {
      PrintLineDebugMenu($"JSON XX:" + x.ToString(Formatting.Indented));
  }

  doc = new GoogleDoc(parsed!, jsons.ToArray()!);
  doc.id = doc_id;
  //await doc.GetSessionId();
  var text = doc.GetText();
  SetMainText(text.Item1,text.Item2);

  ActiveElement(FeelingLuckyButton,false,true);
  ActiveElement(OpenDebugMenuButton,false,true);
  ActiveElement(OpenDocButton,false,true);
  ActiveElement(docidbox,false,true);
  Title.Text = doc.title;
  ActiveElement(Title,true,true);
  Name.Text = doc.name;
  ActiveElement(Name,true,true);
  var httpclient = new HttpClient();
  var profilepic = await httpclient.GetByteArrayAsync(new Uri(doc.profpicurl));
  var bitstream = new MemoryStream(profilepic);
  ProfilePicture.Source = new Bitmap(bitstream);
  ActiveElement(ProfilePicture,true,true);
    watch.Stop();
  PrintLineDebugMenu($"Document loaded successfully in {watch.ElapsedMilliseconds} ms.");
  SaveKeys.lastopened = doc_id;
  JsonParsing.SaveKeys(SaveKeys);
  var text2 = doc.GetText();
  PrintLineDebugMenu(text2.Item1);
 // File.WriteAllText("items.txt",await NetworkManager.GetRequest("https://drivefrontend-pa.clients6.google.com/v1/items:list"/*"[25,\"https://docs.google.com/document/u/0/?usp=docs_web\",25,\"en\",\"ca\",1,null,0,0,\"\",\"\",1,0,null,72175901,[[1,9,13],0,1,1],[1],null,0,1,\"CAMSIhUn9NL9N67auQayvgTkiQWnBp6WpgL58FLkoEXMsBP6gR0=\",{\"1001\":1}]"*/,true));
  // File.WriteAllText("BindTest.txt",await NetworkManager.GetRequest(
 // "https://docs.google.com/document/d/1rbtpzc2QUrT0nT60ZMSlELxujgHzw2UUxn3xmu7z2pI/bind?id=1rbtpzc2QUrT0nT60ZMSlELxujgHzw2UUxn3xmu7z2pI&sid=5e31d1095e7c74c5&token=AJagN6Q3L3VTlm0lH1eRvrcxnAZY:1788285353340&ouid=107343423057709043354&includes_info_params=true&cros_files=false&nded=false&VER=8&tab=t.0&lsq=1788285346255&vc=1&c=1&w=1&flr=0&gsi=0&smv=2147483647&smb=[2147483647, oAMQAg==]&cimpl=1&RID=rpc&SID=8EC8391587DD515B&CI=0&AID=2&TYPE=xmlhttp&zx=lwqqs9qya0r7&t=1"));

//File.WriteAllText("MobileBindTest.txt",await NetworkManager.GetRequest($"https://docs.google.com/document/d/{docid}/mobile/bind?id={docid}"));
  if(SaveKeys.bind)
  {
  StartBindThread();

  }
  if (SaveKeys.toolbar)
  {
      ActivePanel(Toolbar, true);
  }
  SelectionRects = new List<Rectangle>();
  PrevSelections = new List<(int, int)>();
  Selections = new List<(int, int)>();
  Selections.Add((5, 100));

  // MainText.Inlines.Add(new Run("Hello World"));

}
catch (HttpRequestException err)
{
    PrintLineDebugMenu(err.Message);
    SetMainText(err.Message);
}
    }

    public void PrintDebugMenu(string s)
    {
        if (SaveKeys.verbose)
        {
            Console.Write(s);
        }

        if (SaveKeys.log)
        {
            DebugMenuTextBlock.Text += s;
            lock (debugLogLock)
            {
                debugmenulog += s;
            }
        }
    }

    public void StartBindThread()
    {
        var thread = new Thread(BindThread);
        thread.Start();
    }
    public async void BindThread()
    {
        //  var token = await doc.GetToken(docid);
       var token = await doc.GetXsrfToken();
    var bindpost = await NetworkManager.PostRequest(JsonParsing.GetBindPostReq(doc_id,UrlConfig) + $"&token={token}", "count=0");
    var jsonobj = JsonParsing.TryParseFirstJsonObject(bindpost, out JContainer? obj) ? obj : new JObject();
    PrintLineDebugMenu(jsonobj.ToString(Formatting.Indented));
    var postsid = jsonobj[0][1][1];
    PrintLineDebugMenu("Post Sid: " + postsid);
   /* PrintLineDebugMenu("ITEMS LIST:");
    var list = await NetworkManager.PostRequest(
        $"https://drivefrontend-pa.clients6.google.com/v1/items:list?key=AIzaSyDl-UL2oekTnhhyaKOSEIX2fYcWIapfhR0&SID={postsid}","[[null,null,null,null,0,null,null,null,[[\"application/vnd.google-apps.document\"],[\"application/vnd.msword\"],[\"application/vnd.ms-word\"],[\"application/vnd.ms-word.document.macroenabled.12\"],[\"application/msword\"],[\"application/vnd.ms-word.document.12\"],[\"application/vnd.openxmlformats-officedocument.wordprocessingml.document\"],[\"application/vnd.google-gsuite.encrypted; content=\\\"application/vnd.google-gsuite.document-blob\\\"\"],[\"application/vnd.google-gsuite.encrypted; content=\\\"application/vnd.google-apps.document\\\"\"]],null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,[1,2]],[50,\"\"]]");
    PrintLineDebugMenu(list);*/
    //PrintLineDebugMenu(BINDPOST);
    /*  File.WriteAllText("bindpost.html", BINDPOST);
      var SID = BINDPOST.SubstringAfter("\"c\",\"").SubstringBefore("\"");
      var lsq = BINDPOST.SubstringAfter("1788").SubstringAfter("1788").SubstringBefore(",");
      PrintLineDebugMenu("Binding to document...");
      PrintLineDebugMenu("BIND TEST");
      var stream = await NetworkManager
          .GetStreamAsync(
              $"https://docs.google.com/document/d/{docid}/bind?id={docid}&includes_info_params=true&cros_files=false&nded=false&VER=8&tab=t.0&vc=1&c=1&w=1&flr=0&gsi=0&cimpl=1&RID=rpc&CI=0&AID=2&TYPE=xmlhttp&t=1" + /*&SID={SID}*/
           /*   $"&token={token}&smv={int.MaxValue}&lsq=1788{lsq}&smb=[{int.MaxValue.ToString()},oAMQAg==]");
       await using var output = File.OpenWrite("BindStream.txt");
          await stream.CopyToAsync(output);*/
      //File.WriteAllText("BindGetRequestTest.txt",await NetworkManager.GetRequest($"https://docs.google.com/document/d/{docid}/bind?id={docid}&includes_info_params=true&cros_files=false&nded=false&VER=8&tab=t.0&vc=1&c=1&w=1&flr=0&gsi=0&cimpl=1&RID=rpc&CI=0&AID=2&TYPE=xmlhttp&zx=lwq349tga0r7&t=1" + $"&SID={SID}&token={token}&smv={int.MaxValue}&lsq=1788{lsq}&smb=[{int.MaxValue.ToString()},oAMQAg==]"));
      //PrintLineDebugMenu("BIND TEST END");
      var aid = jsonobj[jsonobj.Count - 1][0];
      while (true)
      {
          var tmpaid = await BindToDoc(
              $"&SID={postsid}&AID={aid}" /*$"&SID={SID}&token={token}&smv={int.MaxValue}&lsq=1788{lsq}&smb={$"[{int.MaxValue},oAMQAg==]".UrlEncode()}"*/,
              true);
          if (tmpaid != "")
          {
              aid = tmpaid;
          }
      }
    }

    public async Task AnimateHeight(InputElement element, double fullheight, double duration = 0.2, double targetheight = 0, double resolution = 30)
    {
    var delay = duration / resolution;
    var dis = fullheight - targetheight;
    for (int i = 0; i < resolution; i++)
    {
        element.Height = fullheight - (dis/resolution) * i;
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }
    }

    public async Task ActiveElement(InputElement element, bool active,bool animate = false)
    {
        var fullsize = element.Height;
        //Active & Animating
        if (active && animate)
        {
            element.Height = 0;
            element.IsEnabled = true;
            element.IsVisible = true;
            await AnimateHeight(element,fullsize,0.2f,fullsize);
            return;
        }
        //Active & Not Animating
        if (active)
        {
            element.IsEnabled = true;
            element.IsVisible = true;
            return;
        }
        //Not Active & Animating
        if (animate)
        {
            element.Height = fullsize;
            await AnimateHeight(element, fullsize);
            element.IsEnabled = false;
            element.IsVisible = false;
            return;
        }
        //Not Active & Not Animating
        element.IsEnabled = false;
        element.IsVisible = false;
        return;

    }

    public void ActivePanel(Panel panel, bool active)
    {
        panel.IsEnabled = active;
        panel.IsVisible = active;
    }

    public void PrintLineDebugMenu(string s)
    {
        if (SaveKeys.verbose)
        {
            Console.WriteLine(s);
        }
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
        DebugMenuInput.Text = "";
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
                   // PrintLineDebugMenu(DebugReadText);
                }
    }

    private void MainTextKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl:
                ctrl = true;
                PrintLineDebugMenu("CTRL pressed");
                break;
            case Key.LeftShift:
                PrintLineDebugMenu("Shift pressed");
                shift = true;
                break;
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
                Edit? edit = null;
                var pos = CursorManager.GetCursorPosition();
                if (ctrl)
                {
                    PrintLineDebugMenu($"CTRL+{e.Key} pressed.");
                    if (e.Key == Key.S)
                    {
                        doc.Save();
                    }

                    if (e.Key == Key.V)
                    {
                       PrintLineDebugMenu("Paste");
                        var toplevel = TopLevel.GetTopLevel(this);
                        if (toplevel is not null && toplevel.Clipboard is not null)
                        {
                            string? contents = toplevel.Clipboard.TryGetTextAsync().Result;
                            if (contents is not null)
                            {
                                PrintLineDebugMenu($"Pasting {contents} at {pos}");
                            edit = new Edit(EditType.Insert,new []{pos.ToString(),contents});

                            }
                            else
                            {
                                PrintLineDebugMenu("No clipboard contents or the contents is not text");
                            }
                        }
                        else
                        {
                            PrintLineDebugMenu("Toplevel is null or clipboard is null");
                        }
                    }
                    if (edit != null && doc != null)
                    {
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(edit.Params[1].Length, pos);
                        var text = doc.GetText();
                        SetMainText(text.Item1, text.Item2);
                        MainText.UpdateLayout();
                        lock (CursorManager.CursorLock)
                        {
                            CursorManager.SetTextLayout(MainText.TextLayout);
                        }
                        //CursorManager.MoveCursor();

                    }
                    break;
                }

                int capitaloffset = 'a' - 'A';
                bool isAlphabet = e.Key >= Key.A && e.Key <= Key.Z;
                bool isNumber = e.Key >= Key.D0 && e.Key <= Key.D9;
                bool isnumpadnum = e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9;
                if(isAlphabet)
                {
                    int letter = e.Key - Key.A + 1 - (shift ? capitaloffset : 0);
                    char c = (char)('a' + letter - 1);
                    string s = CharToString(c);
                    edit = new Edit(EditType.Insert,
                        new string[] { pos.ToString(),s});
                    Program.mainWindow.PrintLineDebugMenu($"Inserting {s} at {pos}");
                    doc.history.Edits.Add(edit);
                    doc.OffsetAltersAfter(1, pos);
                }

                if (isNumber)
                {
                    int letter = e.Key - Key.D0 + 1;
                    char c = (char)('0' + letter - 1);
                    if (shift)
                    {
                        switch (c)
                        {
                            case '1':
                                c = '!';
                                break;
                            case '2':
                                c = '@';
                                break;
                            case '3':
                                c = '#';
                                break;
                            case '4':
                                c = '$';
                                break;
                            case '5':
                                c = '%';
                                break;
                            case '6':
                                c = '^';
                                break;
                            case '7':
                                c = '&';
                                break;
                            case '8':
                                c = '*';
                                break;
                            case '9':
                                c = '(';
                                break;
                            case '0':
                                c = ')';
                                break;
                        }
                        string s = CharToString(c);
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(), s });
                        Program.mainWindow.PrintLineDebugMenu($"Inserting {s} at {pos}");
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(1, pos);
                    }
                    else
                    {
                        string s = CharToString(c);
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(), s });
                        Program.mainWindow.PrintLineDebugMenu($"Inserting {s} at {pos}");
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(1, pos);
                    }
                }

                if (isnumpadnum)
                {
                    int letter = e.Key - Key.NumPad0 + 1;
                    char c = (char)('0' + letter - 1);
                    if (shift)
                    {
                        switch (c)
                        {
                            case '8':
                                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { up = true };
                                CursorManager.KeyDown(Move.Up);
                                break;
                            case '4':
                                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { left = true };
                                CursorManager.KeyDown(Move.Left);
                                break;
                            case '6':
                                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { right = true };
                                CursorManager.KeyDown(Move.Right);
                                break;
                            case '2':
                                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { down = true };
                                CursorManager.KeyDown(Move.Down);
                                break;
                        }
                    }
                    else
                    {
                        string s = CharToString(c);
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(), s });
                        Program.mainWindow.PrintLineDebugMenu($"Inserting {s} at {pos}");
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(1, pos);
                    }
                }

                switch (e.Key)
                {
                    case Key.Enter:
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(),"\n"});
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(1, pos);
                        break;
                    case Key.Tab:
                        edit = new Edit(EditType.Insert,
                            new string[] { pos.ToString(),"\u0009"});
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(1, pos);
                        break;
                    case Key.Back:
                        edit = new Edit(EditType.Delete,
                            new string[] { (pos - 1).ToString(),(pos - 1).ToString()});
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(-1, pos);
                        break;
                    case Key.Delete:
                        edit = new Edit(EditType.Delete,
                            new string[] { (pos).ToString(),(pos).ToString()});
                        doc.history.Edits.Add(edit);
                        doc.OffsetAltersAfter(-1, pos + 1);
                        break;
                }

                if (edit != null && doc != null)
                {
                    var text = doc.GetText();
                    SetMainText(text.Item1, text.Item2);
                    MainText.UpdateLayout();
                    CursorManager.SetTextLayout(MainText.TextLayout);
                    //CursorManager.MoveCursor();


                }

                break;
        }
    }

    private void MainTextKeyUp(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl:
                ctrl = false;
                PrintLineDebugMenu("CTRL released");
                break;
            case Key.LeftShift:
                shift = false;
                PrintLineDebugMenu("Shift released");
                break;
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
            case Key.NumPad4:
                if (shift && CursorManager.numpadkeystate.left)
                {
                    CursorManager.numpadkeystate = CursorManager.numpadkeystate with { left = false };
                    CursorManager.KeyUp(Move.Left);
                }

                break;
            case Key.NumPad6:
                if (shift && CursorManager.numpadkeystate.right)
                {
                    CursorManager.numpadkeystate = CursorManager.numpadkeystate with { right = false };
                    CursorManager.KeyUp(Move.Right);
                }

                break;
            case Key.NumPad8:
            if (shift && CursorManager.numpadkeystate.up)
            {
                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { up = false };
                CursorManager.KeyUp(Move.Up);
            }

            break;
            case Key.NumPad2:
            if (shift && CursorManager.numpadkeystate.down)
            {
                CursorManager.numpadkeystate = CursorManager.numpadkeystate with { down = false };
                CursorManager.KeyUp(Move.Down);
            }

            break;
            default:
                break;
        }
    }

    private void AcceptPerm(object? sender, RoutedEventArgs e)
    {
        SaveKeys.acceptedbrowserscraping = true;
        SaveKeys.hassetup = true;
        JsonParsing.SaveKeys(SaveKeys);
        PermissionPrompt.Close();
    }

    public void UpdateSelections()
    {
        if (Selections == null)
        {
            return;
        }
        if (Selections.Count != SelectionRects.Count)
        {
            while (Selections.Count > SelectionRects.Count)
            {
                var r = new Rectangle();
                r.Height = 20;
                r.VerticalAlignment = VerticalAlignment.Top;
                r.HorizontalAlignment = HorizontalAlignment.Left;
                r.Fill = new SolidColorBrush(Color.FromRgb(20,100,200),.2);
                SelectionParent.Children.Add(r);
                SelectionRects.Add(r);

            }
            while (Selections.Count < SelectionRects.Count)
            {
                var r = SelectionRects[SelectionRects.Count - 1];
                SelectionParent.Children.Remove(r);
                SelectionRects.Remove(r);
            }
        }

        List<int> Changed = new List<int>();
            for (int i = 0; i < Selections.Count; i++)
            {
                if (PrevSelections.Count <= i)
                {
                    Changed.Add(i);
                    continue;
                }
                if (Selections[i] != PrevSelections[i])
                {
                    Changed.Add(i);
                }
            }

            foreach (var i in Changed)
            {
                Vector2 start = CursorManager.GetOffsetFromCharacter(new Vector2(Selections[i].Item1,0));
                if (Selections[i].Item1 == Selections[i].Item2)
                {
                    var r = SelectionRects[i];
                    r.Width = 2;
                    r.Height = 20;
                    var margin = new Thickness(start.X, start.Y, 0, 0);
                    r.Margin = margin;
                    SelectionRects[i] = r;
                }
                else
                {
                    Vector2 end = CursorManager.GetOffsetFromCharacter(new Vector2(Selections[i].Item2,0));
                    var r = SelectionRects[i];
                    r.Width = Math.Abs(start.X - end.X) + 2;
                    r.Height = 20;
                    var margin = new Thickness(start.X + (end.X - start.X), start.Y, 0, 0);
                    r.Margin = margin;
                    SelectionRects[i] = r;
                }

            }

    }

    private void Exit(object? sender, RoutedEventArgs e)
    {
        SaveKeys.acceptedbrowserscraping = false;
        SaveKeys.hassetup = true;
        JsonParsing.SaveKeys(SaveKeys);
        this.Close();
    }
}