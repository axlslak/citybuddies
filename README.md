# CityBuddies

CityBuddies is a deliberately small AOSharp.Clientless host for keeping personal-city buddy characters online.

It has no manager, flipper, banker, navigation, commands, IPC, naming rules, shared-password rules, or managed logout flow. Every entry in `buddies.json` is an independent AO account/character combination, and every configured entry is started concurrently.

## Build

Requirements:

- Visual Studio 2022 with the .NET desktop development workload
- .NET Framework 4.8 targeting pack
- NuGet package restore enabled

Open `CityBuddies.sln`, select `Release`, and build. The portable output is written to `release`.

The build automatically downloads the GameData files matching AOSharp.Clientless 1.0.16, verifies their SHA-256 hashes, and copies them to `release\GameData`. This includes the required `StaticDynelData.bin`. The verified files are cached under `.dependencies`, so later builds can reuse them.

## Configure

Copy `buddies.example.json` to `release\buddies.json`, then replace the examples:

```json
[
  {
    "Username": "account-name",
    "Password": "account-password",
    "Character": "Charactername"
  }
]
```

Add as many entries as required. Entries do not need related account names, passwords, or character names. Do not commit `buddies.json`; it contains account credentials and is ignored by Git.

## Run

Run `Buddies.exe`. It starts every configured login concurrently and keeps the clients alive with automatic reconnect enabled. Press Enter to terminate the executable. There is intentionally no logout or AppDomain-unload workflow.

With no argument, `Buddies.exe` loads `buddies.json`. To keep multiple profiles beside the executable, pass the profile name:

```bat
Buddies.exe family
```

That loads `family.json`. Supplying the extension explicitly also works:

```bat
Buddies.exe family.json
```
