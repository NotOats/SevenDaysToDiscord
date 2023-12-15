# SevenDaysToDiscord
Seven Days To Discord is a mod for sending various game events to a discord server.

### Development Setup

To setup this project for development or custom builds:

- Clone the repo
```sh
    git clone https://github.com/NotOats/SevenDaysToDiscordMod
```

- Copy game assemblies
   1. Find the Dedicated Server steam install location
   1. Naviate to the managed assemblly folder
      This is typically found at `<Install_Folder>\7 Days to Die Dedicated Server\7DaysToDieServer_Data\Managed`
   1. Copy `Assembly-CSharp.dll` and `LogLibrary.dll` into the dependencies folder in the project root.

- Configure settings
   1. Rename `appsettings.example.json` to `appsettings.json`
   1. Change the entry for `WebHookUrl`