using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GoogleDocs;
public enum EditType { Insert, Alter, Multi,Noop,Unknown}
public class Edit
{
    public bool IsSaved = false;
    public Edit(EditType type, string[] Params, bool isSaved = false)
    {
        Type = type;
        this.Params = Params;
        IsSaved = isSaved;
    }
    public Edit(JObject json,bool isSaved = false)
    {
        IsSaved = isSaved;
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
        else if (typestring == "ml") // Not sure code could be mt,mlt or smth
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

    public string FetchSaveString()
    {
        string json = "{" +
               $"\"ty\":\"{GetEditTypeCode(Type)}\"," + // Edit type
               $"{(Type == EditType.Alter ? $"\"st\" : {Params[0]}," : "")}" + //Alteration string type
               $"{(Type == EditType.Alter ? $"\"si\" : {int.Parse(Params[1]) + 1}," : "")}" + // Alteration start index
               $"{(Type == EditType.Alter ? $"\"ei\" : {int.Parse(Params[2]) + 1}," : "")}" + // Alteration end index
               $"{(Type == EditType.Alter ? $"\"sm\" : {Params[3]}," : "")}" + // Alteration property json string
               $"{(Type == EditType.Insert ? $"\"ibi\" : {int.Parse(Params[0]) + 1}," : "")}" + // Insertion index
               $"{(Type == EditType.Insert ? $"\"s\" : \"{Params[1]}\"," : "")}" // Insertion string
               ;
        json = json.TrimEnd(',');
        json += "}";
        IsSaved = true;
        return json;
    }

    private string GetEditTypeCode(EditType type)
    {
        switch (Type)
        {
            case EditType.Alter:
                return "as";
                break;
            case EditType.Insert:
                return "is";
                break;
            case EditType.Multi:
                return "ml";
                break;
            case EditType.Noop:
                return "noop";
                break;
            case EditType.Unknown:
                return "unk";
                break;
        }
        return "unk";
    }
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
                Edits.Add(new Edit(obj,true));
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
    JObject? json2;
    public DocHistory? history;
    public string id;
    public string token;
    public string title;
    public string name;
    public string profpicurl;
    public int revision;
    public GoogleDoc(JObject json1, JObject json2)
    {
        this.json1 = json1;
        this.json2 = json2;
        history = new DocHistory(json2);
        title = json1["me"]["t"].ToString();
        token = json1["me"]["dkd"][10].ToString();
        name = json1["me"]["dkd"][8][0].ToString();
        profpicurl = "https:" + json1["me"]["dkd"][8][2].ToString().SubstringBefore("=");
        revision = int.Parse(json1["r"].ToString());
        Console.WriteLine("Revision: " + revision);
        Console.WriteLine("Title: " + title);
        Console.WriteLine("Token: " + token);
        Console.WriteLine("Name: " + name);
        Console.WriteLine("Profile Pic URL: " + profpicurl);
    }

    public async void Save()
    {
        var unsaved = new List<Edit>();
        foreach (var edit in history.Edits)
        {
            if (!edit.IsSaved)
            {
                unsaved.Add(edit);
                /* remove paceholder*/
               // break;
            }
        }

        string savestring = "[";
        foreach (var edit in unsaved)
        {
            savestring += edit.FetchSaveString() + ",";
        }
        savestring = savestring.TrimEnd(',');
        savestring += "]";
        Console.WriteLine("Saving changes...");
        string rev = "rev=" + revision.ToString();
        string bundle = "bundles=" + $"[{{\"commands\": {savestring},\"sid\":\"{NetworkManager.sid}\",\"reqId\":0}}]";
        rev = rev.UrlEncode();
        bundle = bundle.UrlEncode();
        string data = rev + "&".UrlEncode() + bundle;
        Console.WriteLine(bundle);
        Console.WriteLine(rev);
        Console.WriteLine(data);
        var _token = await GetToken(id);
       // string url = $"https://docs.google.com/document/d/{id}/save?id={id}&token={_token}&smv={int.MaxValue}&smb=[{int.MaxValue.ToString()},oAMQAg==]&vc=1&c=1&w=1&flr=0&includes_info_params=true&cros_files=false&nded=false&tab=t.0";
       string url = $"https://docs.google.com/document/d/{id}/save?id={id}";
       // string url = $"https://drive.google.com/d/{id}/save";
     /* string url =
          "https://docs.google.com/document/d/1-i86NaKKaRuEqCiybyJ0hgLBaKHfUwAbS9WI5uiY2iA/save?id=1-i86NaKKaRuEqCiybyJ0hgLBaKHfUwAbS9WI5uiY2iA&sid=103a78443531e74&vc=1&c=1&w=1&flr=0&smv=2147483647&smb=[2147483647, oAMQAg==]&token=AJagN6RRlhhP_4ODuK0y_9sz8gUJ:1788373213572&ouid=107343423057709043354&includes_info_params=true&cros_files=false&nded=false&tab=t.0";
    */   var net = "<!DOCTYPE html>";
       int count = 0;
        while (net.Contains("html"))
        {
            net = await NetworkManager.PostRequest(url, data, count > 0);
            if (net.Contains("Redirect") && net.Contains("url="))
            {
                url = net.SubstringAfter("url=").SubstringBefore("\">");
                string decoded = System.Net.WebUtility.HtmlDecode(url);
                var uri = new Uri(decoded);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                url = uri.GetLeftPart(UriPartial.Path) + "?" + query.ToString();
            }
            else
            {
                url = "";
            }
            if (url != "")
            {
                Console.WriteLine("Got redirect to: " + url);
                Console.WriteLine("Contents: " + net);
            }

            if (url == "")
            {
                Console.WriteLine("Reached end of redirect chain, end contents: " + net);
                break;
            }

           /* if (count > 0)
            {*/
               // net = await NetworkManager.GetRequest(url,true);
           // }
         /*   else
            {*/

//}

            count++;
        }
        Console.WriteLine("Saved changes: " + net);
        revision++;
    }


    public void OffsetAltersAfter(int offset, int after)
    {
        foreach (var edit in history.Edits)
        {
            if (edit.Type == EditType.Alter)
            {
                int start = Convert.ToInt32(edit.Params[1]);
                int end = Convert.ToInt32(edit.Params[2]);
                if (start - 1 >= after)
                {
                    edit.Params[1] = (start + offset).ToString();
                }
                if (end >= after)
                {
                    edit.Params[2] = (end + offset).ToString();
                }
            }
        }
    }

    public void OffsetAlters(int offset)
    {
        foreach (var edit in history.Edits)
        {
            if (edit.Type == EditType.Alter)
            {
                int start = Convert.ToInt32(edit.Params[1]);
                int end = Convert.ToInt32(edit.Params[2]);
                edit.Params[1] = (start + offset).ToString();
                edit.Params[2] = (end + offset).ToString();
            }
        }
    }
    public async Task<string> GetToken(string doc_id)
    {
       /* Console.WriteLine("Getting session ID...");
        var config = JsonParsing.GetUrlConfig();
        string bindurl = JsonParsing.GetBindPostReq(id,config);

           bindurl += $"&zx={new Random().Next(100000,999999)}{new Random().Next(100000,999999)}";
            bindurl += $"&RID={new Random().Next(10000,99999)}";
        Console.WriteLine("BIND URL:");
        Console.WriteLine(bindurl);
        var response = await NetworkManager.PostRequest(bindurl,"count=0");
        Console.WriteLine("BIND POST RESPONSE:");
        Console.WriteLine(response);
        return "";*/
       var watch2 = new Stopwatch();
       watch2.Start();
       string mainhtml =
           await NetworkManager.GetRequest(
               $"https://docs.google.com/document/d/{doc_id}/edit?tab=t.0");
       watch2.Stop();
       Console.WriteLine($"Main HTML request took {watch2.ElapsedMilliseconds} ms");
       //File.WriteAllText("main.html", mainhtml);
       Console.WriteLine("Parsing main HTML...");
       string token = mainhtml.SubstringAfter("\"token\":\"").SubstringBefore("\"");
       //string smv = mainhtml.SubstringAfter("\"docs-smv\":").SubstringBefore(",");
       Console.WriteLine("MAIN HTML TOKEN: " + token);
     //  Console.WriteLine("MAIN HTML SMV: " + smv);
       return token;
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
                else if (Convert.ToInt32(edit.Params[0]) < content.Length)
                {
                    content = content.Substring(0, Convert.ToInt32(edit.Params[0])) + edit.Params[1].Replace("\\n","\n") + content.Substring(Convert.ToInt32(edit.Params[0]));
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

