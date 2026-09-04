using System;

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

    public static string SubstringAfterLast(this string str, string after)
    {
        int index = str.LastIndexOf(after, StringComparison.Ordinal);
        if (index == -1)
        {
            return "";
        }
        return str.Substring(index + after.Length);
    }

    public static string SubstringBefore(this string str, string before)
    {
        int index = str.IndexOf(before, StringComparison.Ordinal);
        if (index == -1)
        {
            return "";
        }
        return str.Substring(0, index);
    }
    public static string SubstringBeforeLast(this string str, string before)
    {
        int index = str.LastIndexOf(before, StringComparison.Ordinal);
        if (index == -1)
        {
            return "";
        }
        return str.Substring(0, index);
    }
    public static string ReplaceFirst(this string str, string search, string replace)
    {
        int index = str.IndexOf(search, StringComparison.Ordinal);
        if (index == -1)
        {
            return str;
        }
        return str.Substring(0, index) + replace + str.Substring(index + search.Length);
    }

    public static string Base64Encode(this string s)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s));
    }

    public static string UrlDecode(this string s)
    {
        int percentcoding = -1;
        string percentcontent = "";
        string decoded = "";
        foreach (char c in s)
        {
            if (percentcoding > 1)
            {
                percentcoding = -1;
                decoded += Convert.ToChar(int.Parse(percentcontent, System.Globalization.NumberStyles.HexNumber));
                percentcontent = "";
            }
            if (percentcoding != -1)
            {
                    percentcontent += c;
                    percentcoding++;
            }
            else
            {
                if (c == '%')
                {
                    percentcoding++;
                }
                else
                {
                    decoded += c;
                }
            }
        }
      //  Console.WriteLine("RAW: " + s.Substring(0,(int)(s.Length * 0.1f)));
      //  Console.WriteLine("DECODE: " + decoded.Substring(0,(int)(decoded.Length * 0.1f)));
        return decoded;
    }
    public static string UrlEncode(this string s)
    {
        string encoded = "";
        foreach (char c in s)
        {
            if ((c >= 48 && c <= 57) || (c >= 65 && c <= 90)|| (c >= 97 && c <= 122) || c == '~' || c == '.' || c == '-' || c == '_' || c == '=' || c == '&')
            {
            encoded += c;
            }
            else
            {
                string hex = Convert.ToString(c, 16);
                string hexUpper = hex.ToUpper();
                if (hexUpper.Length == 1)
                {
                    hexUpper = "0" + hexUpper;
                }
                else if (hexUpper.Length > 2)
                {
                    while (hexUpper.Length > 2)
                    {
                        encoded += "%" + hexUpper.Substring(0, 2);
                        hexUpper = hexUpper.Substring(2);
                    }

                    if (hexUpper.Length == 1)
                    {
                        hexUpper = "0" + hexUpper;
                        encoded += "%" + hexUpper;
                    }
                    else
                    {
                        encoded += "%" + hexUpper;
                    }
                }
                else
                {
                    encoded += "%" + hexUpper;
                }
            }
        }
        return encoded;
    }
}