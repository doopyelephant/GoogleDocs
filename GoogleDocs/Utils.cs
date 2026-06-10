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
}