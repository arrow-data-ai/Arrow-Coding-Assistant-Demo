## Arrow Local Code Assistant Engine & Gradio UI

This repo contains:

- A **local code assistant engine** that connects to a self‑hosted vLLM deployment of **`codellama/CodeLlama-7b-Instruct-hf`** (`local_code_assistant_engine.py`).
- A **Gradio web UI** for interactive chatting with the assistant (`coding_assistant.py`).

- **Raw LLM calls** to the local vLLM OpenAI‑compatible endpoint.
- **RAG (Retrieval‑Augmented Generation)** over documents in the `knowledge-base` directory using FAISS and HuggingFace embeddings.

The backend engine is hard‑wired to use `codellama/CodeLlama-7b-Instruct-hf` only; the model dropdown in the UI is cosmetic and should be left on “llamacode-7b (Instruct)”.

---

## 1. Prerequisites

- **Python** 3.10+ recommended.
- **GPU + CUDA** capable of running CodeLlama‑7B (e.g., a single-GPU server).
- **Docker** (if you are running vLLM in a container).

Python dependencies (installed via `requirements.txt`) include:

- `langchain`, `langchain-openai`, `langchain-community`, `langchain-huggingface`
- `faiss-cpu` or `faiss-gpu`
- `pypdf`

---

## 2. Python environment setup (uv)

This project is set up to use **`uv`** for fast, reproducible Python environments and dependency management.

1. **Install `uv`** (if you don't have it):

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

2. **Create and use a project environment with `uv`**:

From the repo root:

```bash
# Create a Python 3.10+ environment (once)
uv venv .venv --python 3.10

# Activate it
source .venv/bin/activate
```

3. **Install dependencies with `uv`**:

If you have a `requirements.txt`:

```bash
uv pip install -r requirements.txt
```

Or, to install the core dependencies directly:

```bash
uv pip install "langchain-openai>=0.1.0" "langchain-community>=0.0.30" \
               "langchain-huggingface>=0.0.8" faiss-cpu pypdf gradio gradio-toggle
```

---

## 3. Deploying `codellama/CodeLlama-7b-Instruct-hf` with vLLM

The helper code assumes a local OpenAI‑compatible HTTP endpoint at:

- **Base URL**: `http://localhost:8000/v1`
- **Model name**: `codellama/CodeLlama-7b-Instruct-hf`

### 3.1 Configure your Hugging Face token via `.env`

Create a `.env` file in the repo root:

```bash
cat > .env << 'EOF'
HF_TOKEN=your_huggingface_token_here
EOF
```

Make sure `.env` is **not** committed to git (it should already be in `.gitignore`).

### 3.2 Start vLLM with Docker using `.env`

From the repo root, run:

```bash
docker run --gpus all --shm-size 20g --rm -p 8000:8000 \
  --env-file .env \
  vllm/vllm-openai:latest \
  --model codellama/CodeLlama-7b-Instruct-hf \
  --max-model-len 4096
```

This uses the `HF_TOKEN` value from `.env` inside the container.  
Leave this container running; the Python engine will call it through the OpenAI‑compatible API.

---

## 4. Preparing the knowledge base

Place documents into the `knowledge-base` directory in the repo root. Supported formats:

- `.txt`
- `.md`
- `.pdf`
- `.jsonl` (with a `text` field per line)

The RAG helper will:

- Load all supported files.
- Split them into chunks.
- Build an in‑memory FAISS index for each run.

---

## 5. Using the engine

In your own Python code, you can import and call the helpers:

```python
from local_code_assistant_engine import vllm_rag_inference, vllm_llm_inference

# Plain LLM call (no RAG)
res = vllm_llm_inference(model="any-string", query="Explain std::unique_ptr in C++.")
print(res["response"])

# RAG‑augmented call over knowledge-base docs
rag_res = vllm_rag_inference(model="any-string", query="How does our internal build system work?")
print(rag_res["response"])
print(rag_res["metrics"])
```

Notes:

- The `model` argument is ignored; the engine always uses `codellama/CodeLlama-7b-Instruct-hf`.
- Metrics include token estimate, elapsed time, and tokens‑per‑second.

---

## 6. Running ad‑hoc tests (engine only)

From the repo root, with vLLM running and venv activated, you can start a Python REPL:

```bash
python
```

Then:

```python
from local_code_assistant_engine import vllm_llm_inference
out = vllm_llm_inference("codellama/CodeLlama-7b-Instruct-hf", "Write a simple C++ RAII wrapper example.")
print(out["response"])
print(out["metrics"])
```

If everything is wired correctly, you should see a C++ answer plus timing statistics.

---

## 7. Running the Gradio UI

The main interactive experience is provided by `coding_assistant.py`, which builds a Gradio app with:

- A **model dropdown** (keep it on “llamacode-7b (Instruct)”; the backend is fixed to that model).
- A **“Use Knowledge Base” toggle** (enables/disables RAG over the `knowledge-base` folder).
- A **Demo Prompts** accordion for quickly inserting example questions.
- A **Retrieved Sources** accordion that shows which files were used when answering RAG queries.

To launch the UI (with vLLM already running on port 8000 and your venv activated):

```bash
python coding_assistant.py
```

By default, this will start Gradio on:

- **Host**: `0.0.0.0`
- **Port**: `7860`

Open `http://localhost:7860` in your browser to use the assistant.

### RAG vs non‑RAG modes

- **RAG enabled (“Use Knowledge Base” ON)**:
  - Queries go through `vllm_rag_inference`, which:
    - Retrieves up to 4 relevant chunks from `knowledge-base`.
    - Builds a CodeLlama `[INST]` prompt including those chunks as context.
  - The UI’s **Retrieved Sources** panel lists the files and previews used.

- **RAG disabled (“Use Knowledge Base” OFF)**:
  - The engine clears any stored sources and calls `vllm_llm_inference` directly.
  - Answers are based only on the model’s built‑in knowledge; no document context is used.

In both cases, responses are streamed character‑by‑character in the chat and end with a short performance summary (tokens, time, and tokens/sec).

