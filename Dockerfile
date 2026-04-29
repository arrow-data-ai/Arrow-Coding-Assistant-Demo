FROM python:3.11-slim AS builder

RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential \
        git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .

# Fresh pip avoids slow dependency backtracking on unpinned packages.
RUN pip install --no-cache-dir -U "pip>=24.0"

# Install deps: swap faiss-gpu for faiss-cpu (GPU inference is in the vLLM
# container) and drop packages the app never imports (vllm, jq).
RUN pip install --no-cache-dir --prefix=/install \
    $(grep -vE '^(faiss-gpu|vllm|jq)' requirements.txt) \
    faiss-cpu

# ── runtime stage ──────────────────────────────────────────────────────────
FROM python:3.11-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
        libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /install /usr/local

WORKDIR /app

COPY local_code_assistant_engine.py coding_assistant.py ./
COPY rag_diagram.png* ./

# knowledge-base is expected as a bind-mount at runtime
RUN mkdir -p knowledge-base

ENV VLLM_BASE_URL=http://localhost:8000/v1
ENV GRADIO_SERVER_NAME=0.0.0.0

EXPOSE 7860

CMD ["python", "coding_assistant.py"]
