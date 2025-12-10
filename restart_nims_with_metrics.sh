#!/bin/bash
# Script to restart NIM containers with per-request metrics enabled

# Source API keys if available
if [ -f "keys.env" ]; then
fi

# Check if NGC_API_KEY is set
if [ -z "$NGC_API_KEY" ]; then
    echo "⚠️  Warning: NGC_API_KEY not set. Containers may fail to start."
    echo "   Set it in keys.env or export it before running this script."
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "=========================================="
echo "Restarting NIMs with Per-Request Metrics"
echo "GPU Allocation:"
echo "  - 70B: GPUs 0-3"
echo "  - 34B: GPUs 4-5"
echo "  - 13B: GPUs 6-7"
echo "=========================================="
echo ""

# Stop existing NIM containers
echo "Stopping existing NIM containers..."
docker stop vibrant_mclean epic_leakey serene_payne 2>/dev/null || true
docker rm vibrant_mclean epic_leakey serene_payne 2>/dev/null || true

echo ""
echo "Starting NIMs with NIM_PER_REQ_METRICS_ENABLE=1..."
echo ""

# Start CodeLlama 70B on port 8000 (GPUs 0-3)
echo "Starting CodeLlama 70B on port 8000 (GPUs 0-3)..."
docker run -d \
  --name serene_payne \
  --gpus '"device=0,1,2,3"' \
  -p 8000:8000 \
  -e NIM_PER_REQ_METRICS_ENABLE=1 \
  -e NGC_API_KEY="${NGC_API_KEY}" \
  nvcr.io/nim/meta/codellama-70b-instruct:latest

# Start CodeLlama 34B on port 8001 (GPUs 4-5)
echo "Starting CodeLlama 34B on port 8001 (GPUs 4-5)..."
docker run -d \
  --name epic_leakey \
  --gpus '"device=4,5"' \
  -p 8001:8000 \
  -e NIM_PER_REQ_METRICS_ENABLE=1 \
  -e NGC_API_KEY="${NGC_API_KEY}" \
  nvcr.io/nim/meta/codellama-34b-instruct:latest

# Start CodeLlama 13B on port 8002 (GPUs 6-7)
echo "Starting CodeLlama 13B on port 8002 (GPUs 6-7)..."
docker run -d \
  --name vibrant_mclean \
  --gpus '"device=6,7"' \
  -p 8002:8000 \
  -e NIM_PER_REQ_METRICS_ENABLE=1 \
  -e NGC_API_KEY="${NGC_API_KEY}" \
  nvcr.io/nim/meta/codellama-13b-instruct:latest

echo ""
echo "Waiting for containers to start (this may take a minute for large models)..."
sleep 10

echo ""
echo "=========================================="
echo "NIM Deployment Status"
echo "=========================================="
docker ps --filter "name=vibrant_mclean|epic_leakey|serene_payne" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "Checking for exited containers..."
exited=$(docker ps -a --filter "name=vibrant_mclean|epic_leakey|serene_payne" --filter "status=exited" --format "{{.Names}}")
if [ -n "$exited" ]; then
    echo "  ⚠️  Some containers exited. Checking logs..."
    for container in $exited; do
        echo "  📋 Logs for $container:"
        docker logs --tail 20 $container 2>&1 | head -10
        echo ""
    done
fi

echo ""
echo "Verifying metrics are enabled (only for running containers)..."
for container in vibrant_mclean epic_leakey serene_payne; do
    # Check if container exists and is running
    if docker ps --format "{{.Names}}" | grep -q "^${container}$"; then
        if docker exec $container env 2>/dev/null | grep -q "NIM_PER_REQ_METRICS_ENABLE=1"; then
            echo "  ✅ $container: Running with metrics enabled"
        else
            echo "  ⚠️  $container: Running but metrics env var not found"
        fi
    elif docker ps -a --format "{{.Names}}" | grep -q "^${container}$"; then
        echo "  ❌ $container: Container exists but is not running (check logs with: docker logs $container)"
    else
        echo "  ❌ $container: Container not found"
    fi
done

echo ""
echo "=========================================="
echo "NIMs restarted with per-request metrics!"
echo "=========================================="
echo ""
echo "The metrics will now be available in the API responses."
echo "Check the UI after making an inference to see tokens/second metrics."

