using System.Collections.Generic;

namespace GoogleDocs;

public struct BrowserCookie
{
    public string host;
    public string name;
    public string value;
}

public struct BrowserCookieJar
{
    public List<BrowserCookie> cookies;
}

public struct UrlConfig
{
    public int version;
    public string docidkey;
    public string bindurl;
    public string initurl;
}
public struct BrowserCookieKey
{
    public string key;
    public string name;
}

public struct BrowserCookieConfig
{
    public string name;
    public string winpath;
    public string linpath;
    public string type;
}
public struct BrowserCookiePaths
{
    public int version;
    public List<BrowserCookieKey> keys;
    public List<BrowserCookieConfig> browsers;
}