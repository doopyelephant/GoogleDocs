using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net;

namespace GoogleDocs;

public static class Program
{
    public static MainWindow? mainWindow;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
/*[DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;
*/


    [STAThread]
    public static void Main(string[] args)
    {
//AttachConsole(ATTACH_PARENT_PROCESS);

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public async static void DebugMenu()
    {

        List<string> Names = new List<string>();
        Names.Add("Analyse Cookie Header");
        WriteLine("GOOGLE DOCS DEBUG MENU");
        WriteLine("Please enter a command:");
        int i = 1;
        foreach (var n in Names)
        {
            WriteLine(i + ": " + n);
            i++;
        }
        Console.WriteLine("Reading Command...");
        string command = await Read();
        Console.WriteLine("Command: " + command);
        int index = Convert.ToInt32(command);
        switch (index)
        {
            case 1:
                Console.WriteLine("Analysing Cookie Header...");
                await AnalyseCookieHeader();
                break;
        }
        WriteLine("Press any key to exit...");
        await Read();
    }

    public async static Task AnalyseCookieHeader()
    {
        var SaveKeys = JsonParsing.GetSaveKeys();
        WriteLine("Please Paste Cookie Header: ");
        string cookie = await Read();
        WriteLine("");
        var cookies = cookie.Split(';');
        var cookienames = new List<string>();
        var cookievalues = new List<string>();
        cookienames = cookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.Split('=')[0];
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[0];
                }
                return "?";
            }
        ).ToList();
        cookievalues = cookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.Split('=')[1];
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[1];
                }
                return "?";
            }
        ).ToList();
        WriteLine("Name                     Value     Listed");
        for (int i = 0; i < cookienames.Count; i++)
        {
            var truncatedvalue = "";
            if (cookievalues[i].Length > 5)
            {
                truncatedvalue = cookievalues[i].Substring(0, 5) + "...";
            }
            else
            {
                truncatedvalue = cookievalues[i] + "   ";
                for (int k = 0; k < 5 - cookievalues[i].Length; k++)
                {
                    truncatedvalue += " ";
                }
            }
            var whitespace = "";
            for (int j = 0; j < 25 - cookienames[i].Length; j++)
            {
                whitespace += " ";
            }
            WriteLine(cookienames[i] + whitespace + truncatedvalue + "  " + (SaveKeys.attachcookies.Contains(cookienames[i].Trim()) ? "LISTED" : "XXXXXX"));
        }
        WriteLine("");
        WriteLine("Listed but not attached:");
        for (int i = 0; i < SaveKeys.attachcookies.Count; i++)
        {
            if (cookienames.Contains(SaveKeys.attachcookies[i].Trim()) == false)
            {
                WriteLine(SaveKeys.attachcookies[i].Trim());
            }
        }
        WriteLine("");
        bool missing = false;
        foreach (var name in cookienames)
        {
            if (SaveKeys.attachcookies.Contains(name.Trim()) == false)
            {
                missing = true;
            }
        }
        if (missing)
        {
            WriteLine("Add missing cookies to list?");
            WriteLine("1: Yes");
            WriteLine("2: No");
            string add = await Read();
            if (add == "1")
            {
                foreach (var name in cookienames)
                {
                    if (SaveKeys.attachcookies.Contains(name.Trim()) == false)
                    {
                        SaveKeys.attachcookies.Add(name.Trim());
                    }
                }

                WriteLine("Added.");
            }

            WriteLine("");
        }

        WriteLine("Write to Mask?");
        WriteLine("1: Yes");
        WriteLine("2: No");
        string mask = await Read();
        if (mask == "1")
        {
            SaveKeys.enablemask = true;
        }
        else
        {
            SaveKeys.enablemask = false;
        }

        SaveKeys.mask = new List<bool>();
        for (int i = 0; i < SaveKeys.attachcookies.Count; i++)
        {
            if (cookienames.Select(s => s.Trim()).Contains(SaveKeys.attachcookies[i].Trim()))
            {
                SaveKeys.mask.Add(true);
            }
            else
            {
                SaveKeys.mask.Add(false);
            }
        }
        JsonParsing.SaveKeys(SaveKeys);
        WriteLine("Saved.");
        WriteLine("Comparing with generated cookie...");
        CookieManager.InitCookies(SaveKeys);
        var generatedcookie = CookieManager.GetCookie(true);
        if (generatedcookie == cookie)
        {
            WriteLine("SUCCESS: Cookie is Identical.");
        }
        else
        {
            var gencookies = generatedcookie.Split(';');
               var cookievaluesuser = cookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.SubstringAfter("=").Trim();
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[1].Trim();
                }
                return "?";
            }
        ).ToList();
         var cookievaluesgen = gencookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.SubstringAfter("=").Trim();
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[1].Trim();
                }
                return "?";
            }
        ).ToList();
            WriteLine("Generated Cookie:");
            WriteLine(" ");
          PrintCookieTable(generatedcookie);
            WriteLine("");
            WriteLine("User Cookie:");
            WriteLine(" ");
            PrintCookieTable(cookie);
        //Alphabetically sort both lists
       // var sorteduser = cookienames.Select((s, index) => new { Name = s.Trim(), Value = cookievaluesuser[index] }).OrderBy(s => s.Name).ToList();
      //  var sortedgen = SaveKeys.attachcookies.Select((s, index) => new { Name = s.Trim(), Value = cookievaluesgen[index] }).OrderBy(s => s.Name).ToList();
        var sorteduser = cookievaluesuser.OrderBy(x => x);
        var sortedgen = cookievaluesgen.OrderBy(x => x);
        bool identical = true;
        
        for(int i = 0; i < sorteduser.Count(); i++)
        {
            if(i >= sortedgen.Count())
            {
                WriteLine("Difference found: User cookie has more cookies than generated cookie.");
                identical = false;
                break;
            }
            if(sorteduser.ElementAt(i) != sortedgen.ElementAt(i))
            {
                WriteLine("Difference found: " + sorteduser.ElementAt(i) + " != " + sortedgen.ElementAt(i));
                identical = false;
            }
        }
        if (identical)        {
            WriteLine("SUCCESS: Cookie is Identical (ignoring order).");
        }
        else{
            WriteLine("Cookie is different.");
        }
            WriteLine("WARNING: Cookie is different.");
            WriteLine("Analysis: ");
            WriteLine("");
            WriteLine("Generated Cookie:");
            WriteLine(" ");
            PrintCookieTable(generatedcookie);
            WriteLine("");
            WriteLine("User Cookie:");
            WriteLine(" ");
            PrintCookieTable(cookie);
            WriteLine("");
            WriteLine("Send Test Auth Request with Cookie?");
            WriteLine("1: Generated Cookie");
            WriteLine("2: User Cookie");
            WriteLine("3: No");
            var test = await Read();
            var result = false;
            var istest = false;
            if(test == "1")
            {
                istest = true;
                CookieManager.GetCookie(true);
                result = await CookieManager.CookieValidate();
            }
            if(test == "2")
            {
                istest = true;
                CookieManager.CookieOvveride(cookie);
                CookieManager.OvverideInit();
                CookieManager.OvverideAlphabetical(false);
                result = await CookieManager.CookieValidate();
            }
            if(istest)
            {
                if(result)
                {
                    WriteLine("SUCCESS: Cookie is Valid.");
                }
                else
                {
                    WriteLine("FAILURE: Cookie is Invalid.");
                }
            }

        }


    }

    public static void PrintCookieTable(string cookie)
    {
        var cookies = cookie.Split(';');
        var cookienames = new List<string>();
        var cookievalues = new List<string>();
        cookienames = cookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.Split('=')[0];
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[0];
                }
                return "?";
            }
        ).ToList();
        cookievalues = cookies.Select((string s) =>
            {
                if (s.Split("=").Length >= 2)
                {
                    return s.Split('=')[1];
                }
                else if (s.Split("/").Length >= 2)
                {
                    return s.Split('/')[1];
                }
                return "?";
            }
        ).ToList();
        WriteLine("Name                     Value");
        for (int i = 0; i < cookienames.Count; i++)
        {
            var truncatedvalue = "";
            if (cookievalues[i].Length > 5)
            {
                truncatedvalue = cookievalues[i].Substring(0, 5) + "...";
            }
            else
            {
                truncatedvalue = cookievalues[i] + "   ";
                for (int k = 0; k < 5 - cookievalues[i].Length; k++)
                {
                    truncatedvalue += " ";
                }
            }
            var whitespace = "";
            for (int j = 0; j < 25 - cookienames[i].Length; j++)
            {
                whitespace += " ";
            }
            WriteLine(cookienames[i] + whitespace + truncatedvalue);
        }
    }


    private static async Task<string> Read()
    {
        Console.WriteLine("Reading...");
        string s = await mainWindow?.ReadDebugMenu();
        Console.WriteLine("Read: " + s);
        return s;
    }

    private static async Task WriteLine(string s)
    {
        Console.WriteLine(s);
        mainWindow?.PrintLineDebugMenu(s);
    }

    private static void Write(string s)
    {
        mainWindow?.PrintDebugMenu(s);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}