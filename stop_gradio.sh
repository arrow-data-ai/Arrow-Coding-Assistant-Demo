#!/bin/bash
# Stop script for Gradio application

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PID_FILE="$SCRIPT_DIR/gradio.pid"

if [ -f "$PID_FILE" ]; then
    PID=$(cat "$PID_FILE")
    if ps -p $PID > /dev/null 2>&1; then
        kill $PID
        rm "$PID_FILE"
        echo "Gradio application stopped (PID: $PID)"
    else
        echo "Process $PID is not running"
        rm "$PID_FILE"
    fi
else
    echo "PID file not found. Trying to find and kill any running Gradio processes..."
    pkill -f "coding_assistant.py"
    echo "Attempted to stop all coding_assistant.py processes"
fi