#!/bin/bash

# Valheim Mod Build & Deploy Script
# Usage: ./build.sh [release] [run] [monitor]

set -e  # Exit on any error

# Configuration
MOD_NAME="Template"
VERSION="0.1.0"
VALHEIM_PATH="/Users/[USERNAME]/Library/Application Support/Steam/steamapps/common/Valheim"
BEPINEX_PLUGINS="$VALHEIM_PATH/BepInEx/plugins"
RUN_SCRIPT="$VALHEIM_PATH/run_bepinex.sh"

# Parse arguments
CONFIGURATION="Debug"
RUN_GAME=false
MONITOR_LOGS=false

for arg in "$@"; do
    case $arg in
        release)
            CONFIGURATION="Release"
            echo "🚀 Building in Release mode..."
            ;;
        run)
            RUN_GAME=true
            ;;
        monitor)
            MONITOR_LOGS=true
            ;;
        *)
            echo "Usage: ./build.sh [release] [run] [monitor]"
            echo "  release - Build in Release mode (default: Debug)"
            echo "  run     - Launch modded Valheim after deployment"
            echo "  monitor - Start log monitoring after deployment"
            exit 1
            ;;
    esac
done

if [ "$CONFIGURATION" = "Debug" ]; then
    echo "🔨 Building in Debug mode..."
fi

# Clean and build
echo "🧹 Cleaning previous build..."
dotnet clean --configuration $CONFIGURATION --verbosity quiet

echo "📦 Building $MOD_NAME..."
dotnet build --configuration $CONFIGURATION

# Check if build succeeded
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fi

# Copy to BepInEx plugins folder
DLL_FILE="bin/$CONFIGURATION/$MOD_NAME.$VERSION.dll"
if [ -f "$DLL_FILE" ]; then
    echo "📂 Installing to BepInEx plugins folder..."
    cp "$DLL_FILE" "$BEPINEX_PLUGINS/"
    echo "✅ Mod installed: $BEPINEX_PLUGINS/$MOD_NAME.$VERSION.dll"
else
    echo "❌ DLL file not found: $DLL_FILE"
    exit 1
fi

# Start log monitoring if requested
if [ "$MONITOR_LOGS" = true ]; then
    # Kill any existing monitor processes first
    echo "🛑 Stopping any existing log monitors..."
    pkill -f monitor-logs.sh 2>/dev/null || true
    sleep 1
    
    if [ "$RUN_GAME" = true ]; then
        echo "🔍 Log monitoring will start after game launch..."
    else
        echo "🔍 Starting log monitor..."
        ./monitor-logs.sh &
        MONITOR_PID=$!
        echo "📊 Log monitor started (PID: $MONITOR_PID)"
        echo "💡 Check logoutput.log for BepInEx logs"
    fi
fi

# Run modded Valheim if requested
if [ "$RUN_GAME" = true ]; then
    if [ -f "$RUN_SCRIPT" ]; then
        echo "🎮 Launching modded Valheim..."
        cd "$VALHEIM_PATH"
        
        # Start log monitor in background if requested
        if [ "$MONITOR_LOGS" = true ]; then
            cd - > /dev/null
            # Kill any existing monitor processes first
            pkill -f monitor-logs.sh 2>/dev/null || true
            sleep 1
            ./monitor-logs.sh &
            MONITOR_PID=$!
            echo "📊 Log monitor started (PID: $MONITOR_PID)"
            cd "$VALHEIM_PATH"
        fi
        
        ./run_bepinex.sh
    else
        echo "⚠️  run_bepinex.sh not found at: $RUN_SCRIPT"
        exit 1
    fi
else
    echo "🎮 Ready to test in Valheim!"
    echo "💡 Usage options:"
    echo "   ./build.sh run        - Build + deploy + launch game"
    echo "   ./build.sh monitor    - Build + deploy + start log monitoring"
    echo "   ./build.sh run monitor - Build + deploy + launch game + monitor logs"
fi 