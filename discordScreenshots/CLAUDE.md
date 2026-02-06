# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Valheim BepInEx mod that captures in-game screenshots and sends them to Discord via webhooks. The mod supports automatic death screenshots, hotkey-triggered screenshots, and console commands for Discord interaction.

**BepInEx Plugin GUID:** `warpalicious.discordScreenshots`

## Build & Deploy

```bash
# Build (Debug) and install to BepInEx plugins folder
./build.sh

# Build in Release mode
./build.sh release

# Build + launch modded Valheim
./build.sh run

# Build + start log monitoring (writes to logoutput.log)
./build.sh monitor

# Build + launch + monitor
./build.sh run monitor
```

The build uses `dotnet build` targeting .NET Framework 4.7.2. The output DLL includes the version in its name (e.g., `discordScreenshots.1.4.0.dll`).

**Version** is tracked in three places that must stay in sync: `discordScreenshots.csproj` (AssemblyVersion + Version), `Source/Plugin.cs` (ModVersion), and `build.sh` (VERSION).

## Architecture

### Harmony Patches (Source/Discord/)

All game behavior modifications use Harmony patches on Valheim classes:

- **PlayerDeathScreenshotPatch.cs** — Two-phase death screenshot system. `PlayerRagdollScreenshotPatch` hooks `Humanoid.OnRagdollCreated` to capture a screenshot when the player dies (stored as a `Texture2D` in static fields). `PlayerRespawnScreenshotPatch` hooks `Player.Awake` to process and upload the stored screenshot on respawn, since the scene reloads between death and respawn.
- **HotkeyScreenshotPatch.cs** — Hooks `Player.Update` to check for the configurable hotkey (default F12) and fire-and-forget a screenshot upload.
- **DiscordConsoleCommandPatch.cs** — Hooks `Terminal.InitTerminal` to register three console commands: `discord`, `discordtest`, `discordscreenshot`.
- **RemoveDamageFlashOnDeath.cs** — Prefix patch on `Player.OnDamaged` that skips the damage flash when the hit would kill the player (so death screenshots look clean).

### Core Components

- **SimpleDiscordWebhook.cs** — Handles all Discord webhook communication. Captures screenshots via `ScreenCapture.CaptureScreenshotAsTexture()` on the Unity main thread, encodes to PNG, then uploads via multipart form data on a background thread. Uses `System.Net.WebRequest` (not HttpClient).
- **BepinExConfiguration.cs** — All config entries (webhook URL, username, avatar URL, death message, screenshot hotkey). Config is synced between server/clients where noted.
- **Plugin.cs** — BepInEx entry point. Applies all Harmony patches via `HarmonyInstance.PatchAll()` and sets up a `FileSystemWatcher` for live config reloading.

### Inactive Code

`ItemViewer.cs` and `PrefabUtils.cs` are entirely commented out — they were a planned in-game item inspection piece using Jotunn's PieceManager. The asset bundle loading in Plugin.cs is also commented out.

## Dependencies

Referenced from local Valheim install (paths configured in `Environment.props`):
- **BepInEx 5** (BepInEx.dll, 0Harmony.dll)
- **Jotunn** (ValheimModding framework — used for config binding extensions, asset loading)
- **Newtonsoft.Json** (via ValheimModding-JsonDotNET plugin — used for webhook JSON serialization)
- Unity engine modules (ScreenCaptureModule, ImageConversionModule, etc.)

`Environment.props` contains all assembly reference paths. Adjust `VALHEIM_INSTALL` there if the Valheim install location changes.

## Key Patterns

- Screenshot capture **must** happen on Unity's main thread; network upload happens on `Task.Run` background threads.
- Death screenshots use a static storage pattern because the player object is destroyed and recreated between death and respawn — the screenshot is captured at ragdoll creation time and uploaded after the new `Player.Awake`.
- Config uses Jotunn's `BindConfig` extension with `synced: true/false` to control server-client sync behavior.
