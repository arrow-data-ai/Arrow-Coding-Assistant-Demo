#!/bin/bash

# Script to stop vLLM Docker containers
# Usage: ./scripts/stop-vllm.sh [all|70b|34b|13b|7b|starcoder]

set -e

cd "$(dirname "$0")/.."

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}Stopping vLLM Docker Containers${NC}"

SERVICE=${1:-all}

case $SERVICE in
    all)
        echo -e "${GREEN}Stopping all vLLM services...${NC}"
        docker compose -f docker-compose.vllm.yml down
        ;;
    70b-instruct|instruct)
        echo -e "${GREEN}Stopping CodeLlama 70B Instruct...${NC}"
        docker compose -f docker-compose.vllm.yml stop codellama-70b-instruct
        docker compose -f docker-compose.vllm.yml rm -f codellama-70b-instruct
        ;;
    70b|base)
        echo -e "${GREEN}Stopping CodeLlama 70B (Base)...${NC}"
        docker compose -f docker-compose.vllm.yml stop codellama-70b
        docker compose -f docker-compose.vllm.yml rm -f codellama-70b
        ;;
    *)
        echo -e "${RED}Unknown service: $SERVICE${NC}"
        echo "Usage: $0 [all|70b-instruct|70b|instruct|base]"
        exit 1
        ;;
esac

echo -e "${GREEN}✓ Services stopped${NC}"

