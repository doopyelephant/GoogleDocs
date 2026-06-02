# Unofficial Google Docs Client
An unofficial Google Doc's Client built using Avalonia in C# that can open Google Docs with authentication from the cookie files of a logged in browser
Heavily Work In Progress, many things might break
## TODO
🕛 Planned
🚧 WIP
### Already Reverse Engineered/Don't need RE but need implementation
- Text Underline Support 🕛
- Text Color Support 🕛
- Text Highlight(with colors) Support 🕛
- Dynamic browser paths(removing hardcoded browser cookie path) 🚧
### Need reverse engineering & Implementation
- Syncing with Google Docs backend (Saving & Realtime Collaboration)(Roadblock is on Google's cookie authentication) 🚧
## Features
- Authenticating with Google's servers using cookie files within a already logged in browser (Currently only Firefox and Firefox based browsers like Zen work)
- Fetching the contents of a document stored in Google's proprietary format(Kix)
- Render the contents of a document using Avalonia
  - Bold Text
  - Italics
  - Tables
- Typing & Moving the Cursor (No saving yet)
## Known Issues
- Zen "works" but sometimes decides it doesnt want to work(very unreliable)(I have 0 clue why this happens)
## AI Disclaimer
I use Github Copilot to write the odd snippet or to bounce ideas off of, but 95% of code is human written
