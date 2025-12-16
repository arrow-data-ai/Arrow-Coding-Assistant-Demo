#!/bin/bash

# Script to check status of vLLM Docker containers
# Usage: ./scripts/status-vllm.sh

cd "$(dirname "$0")/.."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}vLLM Docker Container Status${NC}"
echo ""

docker compose -f docker-compose.vllm.yml ps

echo ""
echo -e "${YELLOW}GPU Usage:${NC}"
nvidia-smi --query-gpu=index,name,memory.used,memory.total,utilization.gpu --format=csv,noheader

echo ""
echo -e "${YELLOW}Health Check:${NC}"

# Check if services are responding
check_endpoint() {
    local port=$1
    local name=$2
    if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
        echo -e "✓ $name (port $port): ${GREEN}Healthy${NC}"
    else
        echo -e "✗ $name (port $port): Not responding"
    fi
}

check_endpoint 8000 "CodeLlama 70B Instruct"
check_endpoint 8001 "CodeLlama 70B (Base)"
check_endpoint 8002 "Llama 3.3 70B Instruct"

