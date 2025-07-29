#!/bin/bash

# Simple utility to stop all log monitor processes

echo "🛑 Stopping all log monitor processes..."
pkill -f monitor-logs.sh 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ Log monitors stopped"
else
    echo "ℹ️  No log monitor processes were running"
fi 