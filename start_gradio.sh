#!/bin/bash
# Start script for Gradio application
# This script runs the application in the background so it persists after closing Cursor

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Activate virtual environment if it exists
if [ -d ".venv" ]; then
    source .venv/bin/activate
fi

# Start the application with nohup (no hang up) so it continues after terminal closes
# Output will be redirected to gradio.log
nohup python3 coding_assistant.py > gradio.log 2>&1 &

# Get the process ID
PID=$!

# Save PID to file for easy stopping later
echo $PID > gradio.pid

echo "Gradio application started!"
echo "Process ID: $PID"
echo "Logs are being written to: gradio.log"
echo "To stop the application, run: ./stop_gradio.sh"
echo "Or manually: kill $PID"
echo ""
echo "The application should be accessible at: http://YOUR_IP_ADDRESS:7860"
