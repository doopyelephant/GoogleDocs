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
    
    public string bindposturl;
    public string initurl;
}
public struct BrowserCookieKey
{
    public string key;
    public string name;
}

public struct AltPath
{
    public string winpath;
    public string linpath;
    public string name;
}

public struct BrowserCookieConfig
{
    public string name;
    public string winpath;
    public string linpath;
    public string type;
    public List<AltPath> altpaths;
}
public struct BrowserCookiePaths
{
    public int version;
    public List<BrowserCookieKey> keys;
    public List<BrowserCookieConfig> browsers;
}
public struct SaveKeys
{
    public bool hasopened;
    public string lastopened;
    public bool acceptedbrowserscraping;
    public bool ovveridecookie;
    public string cookie;
    public List<string> attachcookies;
    public List<string> compass;
    public bool debugmenu;
    public bool enablemask;
    public List<bool> mask;
    public bool bind;
    public bool log;
    public bool usedynamicpaths;
}