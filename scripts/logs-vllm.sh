#!/bin/bash

# Script to view logs from vLLM Docker containers
# Usage: ./scripts/logs-vllm.sh [service-name]

cd "$(dirname "$0")/.."

SERVICE=${1:-}

if [ -z "$SERVICE" ]; then
    echo "Viewing logs for all services..."
    docker compose -f docker-compose.vllm.yml logs -f
else
    case $SERVICE in
        70b-instruct|instruct)
            docker compose -f docker-compose.vllm.yml logs -f codellama-70b-instruct
            ;;
        70b|base)
            docker compose -f docker-compose.vllm.yml logs -f codellama-70b
            ;;
        *)
            docker compose -f docker-compose.vllm.yml logs -f $SERVICE
            ;;
    esac
fi

