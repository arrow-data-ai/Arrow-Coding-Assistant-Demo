#!/bin/bash

# Script to start vLLM Docker containers
# Usage: ./scripts/start-vllm.sh [all|70b|34b|13b|7b|starcoder]

set -e

cd "$(dirname "$0")/.."

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting vLLM Docker Containers${NC}"

# Check if .env.vllm exists
if [ ! -f .env.vllm ]; then
    echo -e "${YELLOW}Warning: .env.vllm not found. Creating from template...${NC}"
    cp .env.vllm.example .env.vllm 2>/dev/null || true
fi

# Load environment variables
if [ -f .env.vllm ]; then
    export $(cat .env.vllm | grep -v '^#' | xargs)
fi

# Check Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running${NC}"
    exit 1
fi

# Check NVIDIA Docker runtime
if ! docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi > /dev/null 2>&1; then
    echo -e "${RED}Error: NVIDIA Docker runtime not available${NC}"
    exit 1
fi

# Parse arguments
SERVICE=${1:-all}

case $SERVICE in
    all)
        echo -e "${GREEN}Starting all vLLM services...${NC}"
        docker compose -f docker-compose.vllm.yml up -d
        ;;
    70b-instruct|instruct)
        echo -e "${GREEN}Starting CodeLlama 70B Instruct...${NC}"
        docker compose -f docker-compose.vllm.yml up -d codellama-70b-instruct
        ;;
    70b|base)
        echo -e "${GREEN}Starting CodeLlama 70B (Base)...${NC}"
        docker compose -f docker-compose.vllm.yml up -d codellama-70b
        ;;
    *)
        echo -e "${RED}Unknown service: $SERVICE${NC}"
        echo "Usage: $0 [all|70b-instruct|70b|instruct|base]"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}✓ Services started${NC}"
echo ""
echo "Check status with: docker compose -f docker-compose.vllm.yml ps"
echo "View logs with: docker compose -f docker-compose.vllm.yml logs -f [service-name]"
echo ""
echo "Endpoints:"
echo "  - CodeLlama 70B Instruct: http://localhost:8000"
echo "  - CodeLlama 70B (Base):   http://localhost:8001"

