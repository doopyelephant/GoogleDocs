# Chromium App-Bound-Encryption
Chrome & other modern Chromium based browsers use *App-Bound-Encryption* to encrypt their cookies. This is the reason why this unofficial Google Docs Client is currently unable to fully decrypt cookies from modern chromium based browsers out of the box on Windows. Chromium encrypts its cookies using the Windows DDAPI(Figure 2). Chromium used to decrypt cookies using the methods implemented in this repository before Chromium 127, this repository should decrypt cookies from chromium versions 127 and below(Figure 1).
### Figure 1 (C4 Bomb: Blowing up Chrome’s Appbound Cookie Encryption, www.cyberark.com/resources/threat-research-blog/c4-bomb-blowing-up-chromes-appbound-cookie-encryption. Accessed 13 July 2026.)
![Figure 1](https://www.cyberark.com/wp-content/uploads/2025/06/old-cookie-protection-flow.png)
### Figure 2 (C4 Bomb: Blowing up Chrome’s Appbound Cookie Encryption, www.cyberark.com/resources/threat-research-blog/c4-bomb-blowing-up-chromes-appbound-cookie-encryption. Accessed 13 July 2026. )
![Figure 2](https://www.cyberark.com/wp-content/uploads/2025/06/appbound-encryption-flow.png)
# Path forward
## On Windows
ABE is in full effect, we would need SYSTEM perms, and try to impersonate Chrome. I plan to integrate [xaitax/Chrome-App-Bound-Encryption-Decryption](https://github.com/xaitax/Chrome-App-Bound-Encryption-Decryption) but this likely will not work forever, it is an arms race with Google and xaitax. Currently it works but likely Google with shut it down eventually.
## On Linux
ABE is only used on Windows, on Linux Chromium uses the system Keychain over DBUS. (Gnome or KWallet) If these are unavailable it uses a completely exposed key in a file(basic encyption). This is very surpassable, it just needs implementation.
