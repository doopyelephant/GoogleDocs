using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DryIoc.ImTools;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;
public enum EditType { Insert, Alter, Multi,Noop,Unknown}
public class Edit
{
    public Edit(EditType type, string[] Params)
    {
        Type = type;
        this.Params = Params;
    }
    public Edit(JObject json)
    {
        string typestring = json["ty"].ToString();
        if (typestring == "is")
        {
            Type = EditType.Insert;
            string[] paramstmp = new string[2];
            paramstmp[0] = (Convert.ToInt32(json["ibi"]) - 1).ToString();
            paramstmp[1] = json["s"].ToString();
            Params = paramstmp;
        }
        else if (typestring == "as")
        {
            Type = EditType.Alter;
            string[] paramstmp = new string[4];
            paramstmp[0] = json["st"].ToString();
            paramstmp[1] = (Convert.ToInt32(json["si"]) - 1).ToString();
            paramstmp[2] = (Convert.ToInt32(json["ei"]) - 1).ToString();
            paramstmp[3] = json["sm"].ToString();
            Params = paramstmp;
        }
        else if (typestring == "ml") // Not sure code
        {
            Type = EditType.Multi;
            string[] paramstmp = new string[0];
            Params = paramstmp;
        }
        else if (typestring == "noop")
        {
            Type = EditType.Noop;
            string[] paramstmp = new string[0];
            Params = paramstmp;
        }
        else
        {
            Type = EditType.Unknown;
            string[] paramstmp = new string[0];
            Params = paramstmp;
        }

    }
    public EditType Type { get; set; }
    public string[] Params;
}
public class DocHistory
{
    public List<Edit> Edits { get; set; }

    public DocHistory(JObject json)
    {
        Edits = new List<Edit>();
        if (json is null) return;

        var edits = json["sc"] as JArray;
        if (edits is null) return;

        foreach (var token in edits)
        {
            if (token is JObject obj)
            {
                Edits.Add(new Edit(obj));
            }
        }
    }
    private static JObject? AsJObject(JToken? token)
    {
        if (token is null)
            return null;

        if (token.Type == JTokenType.Object)
            return (JObject)token;

        // Optional: wrap non\-object token into an object // return new JObject { ["value"] = token };

        return null;
    }
}
public class GoogleDoc
{
    JObject? json1;
    public DocHistory? history;
    public string id;

    public GoogleDoc(JObject json1, JObject json2)
    {
        this.json1 = json1;
        history = new DocHistory(json2);
    }
    public async Task<string> GetSessionId()
    {
        Console.WriteLine("Getting session ID...");
        var config = JsonParsing.GetUrlConfig();
        string bindurl = JsonParsing.GetBindPostReq(id,config);

           bindurl += $"&zx={new Random().Next(100000,999999)}{new Random().Next(100000,999999)}";
            bindurl += $"&RID={new Random().Next(10000,99999)}";
        Console.WriteLine("BIND URL:");
        Console.WriteLine(bindurl);
        var response = await NetworkManager.PostRequest(bindurl);
        Console.WriteLine("BIND POST RESPONSE:");
        Console.WriteLine(response);
        return "";
    }

    public string GetText()
    {
        string content = "";
        foreach (var edit in history.Edits)
        {
            if (edit.Type == EditType.Insert)
            {
                if (Convert.ToInt32(edit.Params[0]) == content.Length)
                {
                    content += edit.Params[1].Replace("\\n","\n");
                }
            }
        }
        int offset = 0;
        foreach(var edit in history.Edits)
        {
            if(edit.Type == EditType.Alter)
            {
                int start = Convert.ToInt32(edit.Params[1]);
                int end = Convert.ToInt32(edit.Params[2]);
                end++;
                if (start >= 1 && end <= content.Length)
                {
                     var wrapstart = "";
                    var wrapend ="";
                    JsonObject alteration = JsonNode.Parse(edit.Params[3]).AsObject();
                    if(alteration.ContainsKey("ts_bd_i"))
                    {
                        var isbd = alteration["ts_bd_i"].GetValue<bool>();
                        if (!isbd && alteration.ContainsKey("ts_bd"))
                        {
                            var bd = alteration["ts_bd"].GetValue<bool>();
                            if (bd)
                            {
                                wrapstart += "<Bl/>";
                                wrapend += "</Bl>";
                                //content = content.Substring(0, start - 1) + "<b>" + content.Substring(start - 1, end - start + 1) + "</b>" + content.Substring(end);
                            }
                        }
                    }
                     if(alteration.ContainsKey("ts_it_i"))
                    {
                        var isbd = alteration["ts_it_i"].GetValue<bool>();
                        if (!isbd && alteration.ContainsKey("ts_it"))
                        {
                            var bd = alteration["ts_it"].GetValue<bool>();
                            if (bd)
                            {
                                wrapstart += "<It/>";
                                wrapend += "</It>";
                                //content = content.Substring(0, start - 1) + "<i>" + content.Substring(start - 1, end - start + 1) + "</i>" + content.Substring(end);
                            }
                        }
                    }
                    if(wrapstart == "" && wrapend == "")
                    {
                        continue;
                    }
                   
                    content = content.Substring(0, start + offset) + wrapstart + content.Substring(start + offset);
                    offset += wrapstart.Length;
                    content = content.Substring(0, end + offset) + wrapend + content.Substring(end + offset);
                    offset += wrapend.Length;
                }
            }
        }
        // Tables
        if(content.Contains("\u0011") && content.Contains("\u0010"))
        {
        int countstart = Regex.Count(content, "\u0010");
        int countend = Regex.Count(content, "\u0011"); 
        content = content.Replace("\u0010", "<Tb/>");
        offset += 4 * countstart;
        content = content.Replace("\u0011","</Tb>");
        offset += 4 * countend;
        }
        return content;
    }
}

