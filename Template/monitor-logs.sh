#!/bin/bash

# Valheim BepInEx Log Monitor
# Streams complete BepInEx LogOutput.log to local logoutput.log

# Configuration
VALHEIM_PATH="/Users/[USERNAME]/Library/Application Support/Steam/steamapps/common/Valheim"
BEPINEX_LOG="$VALHEIM_PATH/BepInEx/LogOutput.log"
LOCAL_LOG="./logoutput.log"

# Colors for terminal output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "🔍 Starting BepInEx log monitor"
echo "📂 Monitoring: $BEPINEX_LOG"
echo "📝 Local log: $LOCAL_LOG"
echo "🛑 Press Ctrl+C to stop"
echo "----------------------------------------"

# Check if BepInEx log exists
if [ ! -f "$BEPINEX_LOG" ]; then
    echo "❌ BepInEx log not found: $BEPINEX_LOG"
    echo "💡 Make sure Valheim with BepInEx has been run at least once"
    exit 1
fi

# Clear local log file for fresh start
> "$LOCAL_LOG"
echo "🧹 Cleared local log file for fresh session"

# Function to handle cleanup on exit
cleanup() {
    echo ""
    echo "🛑 Log monitoring stopped"
    exit 0
}

# Set up signal handler
trap cleanup SIGINT SIGTERM

# Monitor and stream all new log entries
tail -f "$BEPINEX_LOG" | while IFS= read -r line; do
    # Write everything to local log
    echo "$line" >> "$LOCAL_LOG"
    
    # Color-code terminal output based on log level
    if echo "$line" | grep -E "\[Error\]|\[Fatal\]" >/dev/null; then
        echo -e "${RED}$line${NC}"
    elif echo "$line" | grep -E "\[Warning\]" >/dev/null; then
        echo -e "${YELLOW}$line${NC}"
    elif echo "$line" | grep -E "\[Info\]" >/dev/null; then
        echo -e "${GREEN}$line${NC}"
    elif echo "$line" | grep -E "\[Debug\]" >/dev/null; then
        echo -e "${BLUE}$line${NC}"
    else
        echo "$line"
    fi
done 