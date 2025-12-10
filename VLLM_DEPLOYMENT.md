# vLLM Docker Deployment Guide

This guide explains how to deploy multiple CodeLlama models using vLLM in Docker containers.

## Prerequisites

1. **Docker with NVIDIA GPU support**
   ```bash
   # Install Docker
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   
   # Install NVIDIA Container Toolkit
   distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
   curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
   curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list
   sudo apt-get update
   sudo apt-get install -y nvidia-container-toolkit
   sudo systemctl restart docker
   ```

2. **Docker Compose**
   ```bash
   sudo apt-get install docker-compose-plugin
   ```

3. **NVIDIA GPU with sufficient VRAM** (see requirements below)

## GPU Requirements

| Model | VRAM Required | Recommended GPU |
|-------|---------------|-----------------|
| CodeLlama 70B | ~140GB (FP16) | 2x A100 80GB |
| CodeLlama 34B | ~68GB (FP16) | 1x A100 80GB |
| CodeLlama 13B | ~26GB (FP16) | 1x A40 48GB |
| CodeLlama 7B | ~14GB (FP16) | 1x RTX 4090 |
| StarCoder2 15B | ~30GB (FP16) | 1x A40 48GB |

## Setup

1. **Configure environment variables**
   ```bash
   cp .env.vllm.example .env.vllm
   # Edit .env.vllm and add your HuggingFace token
   nano .env.vllm
   ```

2. **Make scripts executable**
   ```bash
   chmod +x scripts/*.sh
   ```

3. **Adjust GPU allocation** (Optional)
   
   Edit `docker-compose.vllm.yml` to match your GPU setup:
   - Modify `CUDA_VISIBLE_DEVICES` for each service
   - Adjust `device_ids` in the deploy section
   - Change `tensor-parallel-size` based on available GPUs

## Usage

### Start All Models
```bash
./scripts/start-vllm.sh all
```

### Start Individual Models
```bash
./scripts/start-vllm.sh 70b    # CodeLlama 70B
./scripts/start-vllm.sh 34b    # CodeLlama 34B
./scripts/start-vllm.sh 13b    # CodeLlama 13B
./scripts/start-vllm.sh 7b     # CodeLlama 7B
./scripts/start-vllm.sh starcoder  # StarCoder2 15B
```

### Check Status
```bash
./scripts/status-vllm.sh
```

### View Logs
```bash
# All services
./scripts/logs-vllm.sh

# Specific service
./scripts/logs-vllm.sh 70b
./scripts/logs-vllm.sh 34b
```

### Stop Services
```bash
# Stop all
./scripts/stop-vllm.sh all

# Stop specific service
./scripts/stop-vllm.sh 70b
```

### Test Endpoints
```bash
# Test specific port
./scripts/test-vllm.sh 8000  # CodeLlama 70B
./scripts/test-vllm.sh 8001  # CodeLlama 34B
./scripts/test-vllm.sh 8002  # CodeLlama 13B
```

## API Endpoints

Once running, the models are available at:

- **CodeLlama 70B**: `http://localhost:8000/v1`
- **CodeLlama 34B**: `http://localhost:8001/v1`
- **CodeLlama 13B**: `http://localhost:8002/v1`
- **CodeLlama 7B**: `http://localhost:8003/v1` (if enabled)
- **StarCoder2 15B**: `http://localhost:8004/v1` (if enabled)

Each endpoint supports OpenAI-compatible API:
- `/v1/models` - List available models
- `/v1/completions` - Text completion
- `/v1/chat/completions` - Chat completion
- `/health` - Health check

## Docker Compose Commands

### Manual Control
```bash
# Start services
docker compose -f docker-compose.vllm.yml up -d

# Stop services
docker compose -f docker-compose.vllm.yml down

# View logs
docker compose -f docker-compose.vllm.yml logs -f

# Check status
docker compose -f docker-compose.vllm.yml ps

# Restart a service
docker compose -f docker-compose.vllm.yml restart codellama-70b

# Pull latest vLLM image
docker compose -f docker-compose.vllm.yml pull
```

## Configuration Options

### GPU Memory Utilization
Adjust `--gpu-memory-utilization` (0.0-1.0) in docker-compose.vllm.yml:
- 0.95 for maximum performance (may cause OOM)
- 0.90 for stable operation (recommended)
- 0.80 for conservative usage

### Context Length
Modify `--max-model-len` to change maximum sequence length:
- Default: 4096 tokens
- Can be increased if you have more VRAM available

### Tensor Parallelism
Change `--tensor-parallel-size` to split model across multiple GPUs:
- 1 = single GPU
- 2 = split across 2 GPUs
- 4 = split across 4 GPUs

### Quantization
Add quantization flag for lower memory usage:
```yaml
command: >
  --model codellama/CodeLlama-70b-Instruct-hf
  --quantization awq  # or gptq
  ...
```

## Troubleshooting

### Out of Memory (OOM)
- Reduce `--gpu-memory-utilization`
- Reduce `--max-model-len`
- Use quantized models
- Increase `tensor-parallel-size`

### Port Already in Use
```bash
# Check what's using the port
sudo lsof -i :8000

# Change port in docker-compose.vllm.yml
```

### Container Won't Start
```bash
# Check logs
docker compose -f docker-compose.vllm.yml logs codellama-70b

# Check GPU availability
nvidia-smi

# Test NVIDIA Docker
docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi
```

### Model Download Issues
- Ensure HF_TOKEN is set in .env.vllm
- Check internet connection
- Verify Hugging Face account has access to the model
- Check disk space in ~/.cache/huggingface

## Performance Optimization

1. **Use SSD/NVMe for model cache**
   ```yaml
   volumes:
     - /fast/ssd/path:/root/.cache/huggingface
   ```

2. **Enable IPC host mode** (already configured)
   - Improves shared memory performance

3. **Use latest vLLM image**
   ```bash
   docker pull vllm/vllm-openai:latest
   ```

4. **Monitor GPU usage**
   ```bash
   watch -n 1 nvidia-smi
   ```

## Integration with Your Code

Your existing `local_code_assistant_engine.py` will work without changes! The vLLM servers expose OpenAI-compatible endpoints that work with ChatNVIDIA client.

The endpoints match your NIM_CONFIG:
```python
NIM_CONFIG = {
    'codellama/codellama-70b-instruct': {'port': 8000, 'base_url': 'http://localhost:8000/v1'},
    'codellama/codellama-34b-instruct': {'port': 8001, 'base_url': 'http://localhost:8001/v1'},
    'codellama/codellama-13b-instruct': {'port': 8002, 'base_url': 'http://localhost:8002/v1'},
}
```

## Production Considerations

1. **Add health checks** (already configured)
2. **Set restart policies** (already configured as `unless-stopped`)
3. **Monitor resource usage**
4. **Set up logging aggregation**
5. **Use specific vLLM version tags** instead of `latest`
6. **Configure rate limiting** if needed
7. **Add authentication** for production use

## Additional Resources

- [vLLM Documentation](https://docs.vllm.ai/)
- [Docker Compose Reference](https://docs.docker.com/compose/)
- [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/)

