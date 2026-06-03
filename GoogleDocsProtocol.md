# Google Docs Protocol
## Authentication
 - Cookie based authentication
 - Cookies are passed into network requests in a header named "Cookie"
 - There are a huge amount of different cookies google uses
 - They are stored across domains, right now i am using ".docs.google.com", "accounts.google.com", and "docs.google.com"
   
 *My list of cookie types are the below(there is probably more)*
 
    "__Secure-1PAPISID"
    
    "__Secure-1PSID"
    
    "__Secure-1PSIDCC"
    
    "__Secure-3PAPISID"
    
    "__Secure-3PSID"
    
    "__Secure-3PSIDCC"
    
    "__Secure-3PSIDTS"
    
    "__Secure-OSID"
    
    "__Secure-STRP"
    
    "GFE_RTT"
    
    "AEC"
    
    "APISID"
    
    "COMPASS"
    
    "HSID"
    
    "NID"
    
    "OSID"
    
    "SAPISID"
    
    "SEARCH_SAMESITE"
    
    "SID"
    
    "SIDCC"
    
    "SSID"
    
    "__Secure.LB"
    
    "__Secure-1PSIDTS"
    
    "__Secure…SIDCC"
    
    "__Secure-BUCKET"
## Operational Transformation
Google Docs uses a model called Operational Transformation(OT), the idea behind OT is that to have realtime collaboration, it is impractical to sync the entire document every time one person types one letter.
So instead of one client saying to all the others: "Here is the new and updated document!", it says "I typed the letter "m" at character 143 Today, June 3rd, 2026, at 7:25 AM and 561 Milliseconds". This way all the clients can then take the delta coming from the other clients and layer it on top of its existing document state. Google's internal format called "Kix" represents these changes in a giant json file. In this giant json file there is a json array of changes, each change has its own type(Insert, Alter, etc) and parameters(Char to Insert, Locations, etc). The changes I have RE'd are below.
|Type     | Shortform |Description                                                                 |
|---------|-----------|----------------------------------------------------------------------------|
|Insert|"is"|Inserts a string at the given index with params "s" for the string to insert, and "ibi" for where to insert(insert begin index)|
|Alter|"as"|Alters text in any way, Bolds, Italics, Colors, Formatting, etc|
|Multi|"ml"|Multiple of the above changes packaged into one(e.g. can be used for Ctrl-Z)|



