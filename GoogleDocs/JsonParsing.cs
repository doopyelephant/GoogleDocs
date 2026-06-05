using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;

public static class JsonParsing
{

// Exact extraction from raw text: finds first {...} while handling nested braces and quoted strings.
public static bool TryExtractFirstJsonObject(
    string input,
    out JObject? obj,
    out int startIndex,
    out int endIndexExclusive,
    out string rawJson)
{
    obj = null;
    startIndex = -1;
    endIndexExclusive = -1;
    rawJson = string.Empty;

    if (string.IsNullOrEmpty(input))
        return false;

    int i = 0;
    while (i < input.Length && input[i] != '{') i++;
    if (i >= input.Length) return false;

    startIndex = i;

    int depth = 0;
    bool inString = false;
    bool escape = false;

    for (; i < input.Length; i++)
    {
        char c = input[i];

        if (inString)
        {
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = false;
            }

            continue;
        }

        if (c == '"')
        {
            inString = true;
            continue;
        }

        if (c == '{')
        {
            depth++;
            continue;
        }

        if (c == '}')
        {
            depth--;
            if (depth == 0)
            {
                endIndexExclusive = i + 1;
                rawJson = input.Substring(startIndex, endIndexExclusive - startIndex);

                try
                {
                    obj = JObject.Parse(rawJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    return false;
}


// Reads: [&]digits&payload  where payload length is exactly 'digits'.
public static bool TryReadLengthPrefixedSegment(
    string input,
    int startIndex,
    out string payload,
    out int nextIndex)
{
    payload = string.Empty;
    nextIndex = startIndex;

    if (input is null || startIndex < 0 || startIndex >= input.Length)
        return false;

    int i = startIndex;

    // Optional whitespace
    while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

    // Optional leading '&'
    if (i < input.Length && input[i] == '&') i++;

    int lenStart = i;
    while (i < input.Length && char.IsDigit(input[i])) i++;

    if (i == lenStart) return false; // no digits
    if (i >= input.Length || input[i] != '&') return false;

    if (!int.TryParse(input.Substring(lenStart, i - lenStart), out int length) || length < 0)
        return false;

    i++; // skip '&' before payload

    if (i + length > input.Length) return false;

    payload = input.Substring(i, length);
    nextIndex = i + length;
    return true;
}







    public static bool TryParseFirstJsonObject(string input, out JObject? obj)
    {
     obj = null;
     if (string.IsNullOrWhiteSpace(input)) return false;

     var start = input.IndexOf('{');
     if (start <0) return false;

     var jsonPart = input[start..];

     using var sr = new StringReader(jsonPart);
     using var reader = new JsonTextReader(sr)
     {
     SupportMultipleContent = true };

     var serializer = JsonSerializer.CreateDefault();

     while (reader.Read())
     {
     if (reader.TokenType == JsonToken.StartObject)
     {
     obj = serializer.Deserialize<JObject>(reader);
     return obj is not null;
     }
     }

     return false;
    }


    public static String InitialReq(String docid,UrlConfig config)
    {
        return config.initurl.Replace(config.docidkey, docid);
    }
    public static String GetBindPostReq(String docid,UrlConfig config)
    {
         return config.bindposturl.Replace(config.docidkey, docid);
    }

    public static String GetBindReq(String docid,UrlConfig config)
    {
        return config.bindurl.Replace(config.docidkey, docid);
    }

    public static void SaveKeys(SaveKeys keys)
    {
        string json = JsonConvert.SerializeObject(keys, Formatting.Indented);
        File.WriteAllText("SaveKeys.json", json);
    }



    public static UrlConfig GetUrlConfig()
    {
        string json = File.ReadAllText("UrlConfig.json");
        var UrlConfig = JsonConvert.DeserializeObject<UrlConfig>(json);
        return UrlConfig;
    }

    public static SaveKeys GetSaveKeys()
    {
        string json = File.ReadAllText("SaveKeys.json");
        var SaveKeys = JsonConvert.DeserializeObject<SaveKeys>(json);
        return SaveKeys;
    }




    public static BrowserCookiePaths GetBrowserCookiePaths()
    {
        string json = File.ReadAllText("BrowserCookiePaths.json");
        var BrowserCookiePaths = JsonConvert.DeserializeObject<BrowserCookiePaths>(json);
        return BrowserCookiePaths;
    }
}