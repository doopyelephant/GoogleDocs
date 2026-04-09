using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace GoogleDocs;

class Program
{

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static void DebugMenu()
    {

        List<string> Names = new List<string>();
        Names.Add("Analyse Cookie Header");
        Console.WriteLine("GOOGLE DOCS DEBUG MENU");
        Console.WriteLine("Please enter a command:");
        int i = 1;
        foreach (var n in Names)
        {
            Console.WriteLine(i + ": " + n);
            i++;
        }
        string command = Console.ReadLine();
        int index = Convert.ToInt32(command);
        switch (index)
        {
            case 1:
                AnalyseCookieHeader();
                break;
        }
    }

    public static void AnalyseCookieHeader()
    {
        var SaveKeys = JsonParsing.GetSaveKeys();
        Console.WriteLine("Please Paste Cookie Header: ");
        string cookie = Console.ReadLine();
        Console.WriteLine("");
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
        Console.WriteLine("Name                     Value     Listed");
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
            Console.WriteLine(cookienames[i] + whitespace + truncatedvalue + "  " + (SaveKeys.attachcookies.Contains(cookienames[i].Trim()) ? "LISTED" : "XXXXXX"));
        }
        Console.WriteLine("");
        Console.WriteLine("Listed but not attached:");
        for (int i = 0; i < SaveKeys.attachcookies.Count; i++)
        {
            if (cookienames.Contains(SaveKeys.attachcookies[i].Trim()) == false)
            {
                Console.WriteLine(SaveKeys.attachcookies[i].Trim());
            }
        }
        Console.WriteLine("");
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
            Console.WriteLine("Add missing cookies to list?");
            Console.WriteLine("1: Yes");
            Console.WriteLine("2: No");
            string add = Console.ReadLine();
            if (add == "1")
            {
                foreach (var name in cookienames)
                {
                    if (SaveKeys.attachcookies.Contains(name.Trim()) == false)
                    {
                        SaveKeys.attachcookies.Add(name.Trim());
                    }
                }

                Console.WriteLine("Added.");
            }

            Console.WriteLine("");
        }

        Console.WriteLine("Write to Mask?");
        Console.WriteLine("1: Yes");
        Console.WriteLine("2: No");
        string mask = Console.ReadLine();
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
        Console.WriteLine("Saved.");
        Console.WriteLine("Comparing with generated cookie...");
        CookieManager.InitCookies(SaveKeys);
        var generatedcookie = CookieManager.GetCookie();
        if (generatedcookie == cookie)
        {
            Console.WriteLine("SUCCESS: Cookie is Identical.");
        }
        else
        {
            Console.WriteLine("WARNING: Cookie is different.");
            Console.WriteLine("Analysis: ");
            Console.WriteLine("");
            Console.WriteLine("Generated Cookie:");
            Console.WriteLine(" ");
            PrintCookieTable(generatedcookie);
            Console.WriteLine("");
            Console.WriteLine("User Cookie:");
            Console.WriteLine(" ");
            PrintCookieTable(cookie);
            Console.WriteLine("");
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
        Console.WriteLine("Name                     Value");
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
            Console.WriteLine(cookienames[i] + whitespace + truncatedvalue);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}