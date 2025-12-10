#!/bin/bash

# Script to test vLLM endpoints
# Usage: ./scripts/test-vllm.sh [port]

PORT=${1:-8000}

echo "Testing vLLM endpoint on port $PORT..."
echo ""

# Test /v1/models endpoint
echo "1. Checking available models:"
curl -s "http://localhost:$PORT/v1/models" | jq '.'
echo ""

# Test completion endpoint
echo "2. Testing completion:"
curl -s "http://localhost:$PORT/v1/completions" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "codellama/CodeLlama-70b-Instruct-hf",
    "prompt": "def fibonacci(n):",
    "max_tokens": 100,
    "temperature": 0.0
  }' | jq '.choices[0].text'

echo ""
echo "Test complete!"

