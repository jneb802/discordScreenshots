# Valheim macOS Modding Template

A template for creating Valheim mods with BepInEx on macOS. Includes automated build scripts, log monitoring, and a rename utility for quick mod creation.

## 📁 Project Structure

```
Template/
├── Source/
│   ├── Plugin.cs              # Main BepInEx plugin class
│   ├── BepinExConfiguration.cs # Configuration helper
│   └── Template.cs             # Template class
├── Properties/
│   └── AssemblyInfo.cs         # Assembly metadata
├── Template.csproj             # C# project file
├── Environment.props           # Valheim/BepInEx path configuration
├── build.sh                    # Build & deploy automation
├── monitor-logs.sh             # Log monitoring utility
├── stop-monitor.sh             # Stop log monitoring
├── rename-mod.sh               # Template renaming utility
└── README.md                   # This file
```

## 🛠️ Prerequisites

- **macOS** with Valheim installed via Steam
- **.NET SDK 9.0+** (`brew install dotnet`)
- **BepInEx** installed in Valheim directory
- **Steam Valheim** at standard location: `~/Library/Application Support/Steam/steamapps/common/Valheim`
- **Publicized Assemblies** via the [AssemblyRePublicizer](https://github.com/shibowo/AssemblyRePublicizer)
- **Jotunn** installed in the Valheim BepinEx folder

### macOS BepInEx Installation

1. **Setup BepInEx folder** - Download [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (Windows version) and install in the Valheim directory
2. **Download** [BepInEx v5.4.23.1 for MacOS](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.1)
3. **Delete** `doorstop_config.ini` from Valheim directory
4. **Delete** contents of Windows `BepInEx/core/` folder (if exists)
5. **Copy** all files from macOS `BepInEx/core/` → your `Valheim/BepInEx/core/`
6. **Copy** `libdoorstop.dylib` from macOS BepInEx zip → your `Valheim/` directory
7. **Download** [run_bepinex.sh](https://gist.github.com/allquixotic/0530bde2247415a676288e8f62592a4d) → put in your `Valheim/` directory
8. **Launch modded Valheim**: Open terminal and cd to Valheim directory
   ```bash
   chmod +x run_bepinex.sh
   ./run_bepinex.sh
   ```

**Note**: Requires Rosetta 2 for Intel compatibility. Install with: `softwareupdate --install-rosetta`

*Source: [GUIDE] Running Mods on MacOS - [Reddit](https://www.reddit.com/r/valheim/comments/1dcko3i/guide_running_mods_on_macos/)*

## 🚀 How to use this template

### 1. **Create Your Mod**
```bash
# Clone or copy this template
cd Template

# Rename template to your mod (e.g., "AwesomeHammer")
./rename-mod.sh AwesomeHammer

# Or with custom author
./rename-mod.sh AwesomeHammer YourUsername
```

### 2. **Verify Paths**
Check `Environment.props` and update the Valheim path if needed:
```xml
<VALHEIM_INSTALL>/Users/USERNAME/Library/Application Support/Steam/steamapps/common/Valheim</VALHEIM_INSTALL>
```

### 3. **Build & Test**
```bash
# Build and deploy to BepInEx
./build.sh

# Build, deploy, and launch Valheim
./build.sh run

# Build, deploy, and start log monitoring
./build.sh monitor

# Build, deploy, launch game, and monitor logs
./build.sh run monitor
```

## 🔧 Included Scripts

### **`build.sh`** - Build & Deploy Automation
```bash
./build.sh [release] [run] [monitor]
```
- Builds your mod (Debug or Release)
- Deploys DLL to BepInEx plugins folder
- Optionally launches Valheim and/or starts log monitoring

### **`rename-mod.sh`** - Template Transformer
```bash
./rename-mod.sh NewModName [Author]
```
- Renames all Template references to your mod name
- Updates namespaces, classes, files, and GUIDs
- Creates a backup and tests the build

### **`monitor-logs.sh`** - Real-time Log Streaming
```bash
./monitor-logs.sh
```
- Streams BepInEx logs with color-coded output
- Saves logs to `logoutput.log` for IDE access
- Clears log file on each session start

### **`stop-monitor.sh`** - Stop Log Monitoring
```bash
./stop-monitor.sh
```
- Stops all running log monitor processes

### **Environment.props**
Contains all Valheim and BepInEx paths:
```xml
<VALHEIM_INSTALL>/path/to/valheim</VALHEIM_INSTALL>
<BEPINEX_PATH>$(VALHEIM_INSTALL)/BepInEx</BEPINEX_PATH>
```
```

## 📄 License

This template is provided as-is for Valheim modding. Respect BepInEx and Valheim's terms of service when distributing mods.