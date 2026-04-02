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
}