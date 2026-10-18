> [!WARNING]
> If you have paid for Unofficial Google Docs, you have been scammed and you should immediately request a refund.  Unofficial Google Docs is entirely free and **is intended for NON-COMMERCIAL use only**, I do not attempt to profit off of it.  \
> \
>  Unofficial Google Docs is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.

> [!NOTE]
> DoopyElephant and Unofficial Google Docs is not affiliated with Google LLC, our project is not official or endorsed by Google LLC in any way. \
> \
> Google, Google LLC and Google Docs are trademarks or registered trademarks of Google LLC in the United States of America and elsewhere.



# Unofficial Google Docs Client
An unofficial Google Doc's Client built using Avalonia in C# that can open Google Docs with authentication from the cookie files of a logged in browser

**Heavily Work In Progress, many things might break**

[Technical Documentation here](GoogleDocsProtocol.md)

## Install
### Windows
Paste this into powershell(press windows, type "powershell", it should show "Windows Powershell", then hit enter):
```
iwr -useb "https://gitlab.com/doopyelephant/GoogleDocs/-/raw/master/Download&Install.ps1" | iex
```
### Linux
Paste this into your terminal:
```
sudo -v && sudo bash -c "$(curl -fsSL "https://gitlab.com/doopyelephant/GoogleDocs/-/raw/master/Download&Install.sh")"
```
## TODO
🕛 Planned
🚧 WIP
🟨 Works but experimental
✅ Complete
### Already Reverse Engineered/Don't need RE but need implementation
- Text Underline Support 🕛
- Text Color Support 🕛
- Text Highlight(with colors) Support 🕛
- Dynamic browser paths(removing hardcoded browser cookie path) ✅
### Need reverse engineering & Implementation
- Syncing with Google Docs backend ✅
## Features
- Authenticating with Google's servers using cookie files within a already logged in browser (Currently only Firefox and Firefox based browsers like Zen work)
- Fetching the contents of a document stored in Google's proprietary format(Kix)
- Render the contents of a document using Avalonia
  - Bold Text
  - Italics
  - Font Size
  - Font Family
- Realtime incoming and outgoing changes to the document, this means if anybody is editing the document, everybody will see the changes in realtime on their own machine
## Known Issues
- Zen "works" but sometimes decides it doesnt want to work(very unreliable)(I have 0 clue why this happens)
- Edge on Windows runs in the background(even when closed) so Google Docs is unable to fetch Edge's Cookies **(Fix: End Task for Edge & Edge for Game Bar in Task Manager)**
- Currently only Firefox works(On Windows) for providing Google auth cookies, this is due to ABE(App-Bound-Encryption), see [ChromiumABE.md](ChromiumABE.md)
## AI Disclaimer
I use Github Copilot to write the odd snippet or to bounce ideas off of (large % of commit messages are AI Generated), but ~95% of code is human written
## Stardance
This is part of [The Stardance Project](https://stardance.hackclub.com)

## Credits
- browser_cookies.py comes from [akkana/scripts](https://github.com/akkana/scripts)
- some RE was done by [James Somers](https://features.jsomers.net/how-i-reverse-engineered-google-docs/)
- Google Fonts Catalog comes from [jonathantneal/google-fonts-complete](https://github.com/jonathantneal/google-fonts-complete/tree/master)
