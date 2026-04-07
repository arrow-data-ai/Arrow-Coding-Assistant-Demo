# AI Coding Assistant — Template

A **local RAG-powered coding assistant** that connects to a self-hosted
[vLLM](https://github.com/vllm-project/vllm) deployment and serves an
interactive Gradio chat UI. Drop your codebase into `knowledge-base/` and the
assistant will ground its answers in your actual source code and docs.

## What's included

| File | Purpose |
|------|---------|
| `local_code_assistant_engine.py` | LLM client, FAISS RAG pipeline, metrics |
| `coding_assistant.py` | Gradio web UI (chat, RAG toggle, source viewer) |
| `Dockerfile` | Multi-stage image for the Gradio app |
| `docker-compose.yml` | Full-stack deploy: vLLM **+** Gradio app |
| `docker-compose.vllm.yml` | Standalone vLLM server (if running the app on the host) |
| `knowledge-base/` | Drop your project source code / docs here |
| `requirements.txt` | Python dependencies |

A **sample knowledge base** (`knowledge-base/sample-app/`) is included so
you can test the RAG pipeline out of the box before plugging in your own code.

---

## Quick start (Docker)

### 1. Create a `.env` file

```bash
cat > .env << 'EOF'
HF_TOKEN=your_huggingface_token_here
TENSOR_PARALLEL_SIZE=2        # number of GPUs for vLLM tensor parallelism
EOF
```

### 2. Add your code to the knowledge base

Replace or extend the sample files in `knowledge-base/` with your own project:

```bash
# Example: clone your repo into the knowledge base
git clone https://github.com/your-org/your-project.git knowledge-base/your-project
```

Supported file types: `.py`, `.js`, `.ts`, `.java`, `.go`, `.rs`, `.cs`, `.sql`,
`.json`, `.yaml`, `.md`, `.txt`, `.pdf`, and
[many more](local_code_assistant_engine.py).

### 3. Start everything

```bash
docker compose up -d
```

This will:

1. Pull `vllm/vllm-openai` and start serving the model.
2. Build the `coding-assistant` image from the repo `Dockerfile`.
3. Wait for vLLM to pass its health check, then start the Gradio UI.

Open **http://localhost:7860** once vLLM finishes loading (first start
downloads ~60 GB of model weights).

### 4. Useful commands

```bash
# Watch vLLM model loading progress
docker compose logs -f vllm

# Rebuild the assistant image after code changes
docker compose up -d --build coding-assistant

# Stop everything
docker compose down
```

---

## Manual setup (without Docker)

### 1. Prerequisites

- **Python 3.10+**
- **GPU** with sufficient VRAM for the model (A100 80 GB recommended)
- **Docker** (for the vLLM server)

### 2. Python environment

```bash
uv venv .venv --python 3.10
source .venv/bin/activate
uv pip install -r requirements.txt
```

### 3. Start vLLM

```bash
# Create .env with your HF token
echo "HF_TOKEN=hf_..." > .env

docker compose -f docker-compose.vllm.yml up -d
docker compose -f docker-compose.vllm.yml logs -f nemotron-3-nano
```

### 4. Run the Gradio UI

```bash
python coding_assistant.py
# → http://localhost:7860
```

---

## Configuration

| Environment variable | Description | Default |
|----------------------|-------------|---------|
| `VLLM_BASE_URL` | OpenAI-compatible endpoint | `http://localhost:8000/v1` |
| `MODEL_ID` | Model served by vLLM | `nvidia/NVIDIA-Nemotron-3-Nano-30B-A3B-BF16` |
| `HF_TOKEN` | Hugging Face token (for gated models) | *(required)* |
| `TENSOR_PARALLEL_SIZE` | GPUs for tensor parallelism | `8` |

---

## Preparing your knowledge base

Place source code and documentation into `knowledge-base/`. The RAG engine
will recursively scan for supported files, split them into chunks, and build
an in-memory FAISS index on first query (and rebuild automatically when files
change).

```
knowledge-base/
├── your-project/
│   ├── src/
│   ├── docs/
│   └── README.md
└── another-project/
    └── ...
```

---

## Using the engine programmatically

```python
from local_code_assistant_engine import vllm_rag_inference, vllm_llm_inference

# Plain LLM call (no RAG)
res = vllm_llm_inference(model="any", query="Explain Python decorators.")
print(res["response"])

# RAG-augmented call grounded in your knowledge base
rag_res = vllm_rag_inference(model="any", query="How does authentication work in our API?")
print(rag_res["response"])
print(rag_res["metrics"])  # {'tokens': ..., 'time': ..., 'tps': ...}
```

---

## Building and pushing the Docker image

```bash
docker build -t coding-assistant .
docker tag coding-assistant ghcr.io/<your-github-user>/coding-assistant:latest
docker push ghcr.io/<your-github-user>/coding-assistant:latest
```

---

## Creating a branch for your project

This `main` branch is a **template**. To create a customized version for a
specific project:

```bash
git checkout -b my-project
# Replace knowledge-base/ contents with your codebase
# Customize prompts in local_code_assistant_engine.py if desired
git add -A && git commit -m "my-project: add knowledge base and customize prompts"
git push -u origin my-project
```

See the `c#` branch for an example of a project-specific configuration.
