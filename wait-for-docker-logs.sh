#!/bin/bash
set -e  # Exit immediately on error

# Check if the correct number of arguments are provided
if [ "$#" -lt 3 ]; then
    echo "Usage: $0 <container_name_or_id> <search_text> <timeout_in_seconds>"
    exit 1
fi

# Variables
CONTAINER_NAME="$1"
SEARCH_TEXT="$2"
TIMEOUT="$3"

echo "Searching logs of container '$CONTAINER_NAME' for text: '$SEARCH_TEXT'..."
echo "Timeout set to $TIMEOUT seconds."

START_TIME=$(date +%s)

# Start `docker logs` in the background and capture its PID
docker logs -f "$CONTAINER_NAME" 2>&1 &
LOGS_PID=$!

# Function to clean up the background process
cleanup() {
    echo "Cleaning up..."
    kill "$LOGS_PID" 2>/dev/null || true
}

# Ensure cleanup is called on script exit
trap cleanup EXIT

while IFS= read -r line; do
    echo "$line"

    # Check if the log line contains the search text
    if [[ "$line" == *"$SEARCH_TEXT"* ]]; then
        echo "Found the specific line: '$SEARCH_TEXT'. Exiting..."
        cleanup
        exit 0
    fi

    # Check for timeout
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - START_TIME))
    if [ "$ELAPSED" -ge "$TIMEOUT" ]; then
        echo "Timeout reached ($TIMEOUT seconds). Exiting..."
        cleanup
        exit 1
    fi
done < <(docker logs -f "$CONTAINER_NAME" 2>&1)