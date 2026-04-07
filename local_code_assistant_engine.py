# Standard library imports
import os 
import re
import time
from collections import Counter
from pathlib import Path


# LLM and embedding model imports
from langchain_openai import ChatOpenAI
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_core.messages import SystemMessage, HumanMessage

# Document loading and processing imports
from langchain_community.document_loaders import TextLoader, PyPDFLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter

# Vector store and QA chain imports
from langchain_community.vectorstores import FAISS
from langchain_classic.chains import RetrievalQA
from langchain_core.prompts import PromptTemplate

# Get the current working directory
working_dir = os.path.dirname(os.path.abspath(__file__))

# Initialize default embedding model 
embeddings = HuggingFaceEmbeddings()

# Global variable to store last retrieved documents for source display
_last_retrieved_docs = []

# Cache the FAISS knowledge base so it's built once, not on every query
_cached_knowledge_base = None
_cached_kb_mtime = None

MODEL_ID = os.environ.get('MODEL_ID', 'nvidia/NVIDIA-Nemotron-3-Nano-30B-A3B-BF16')
VLLM_BASE_URL = os.environ.get('VLLM_BASE_URL', 'http://localhost:8000/v1')

_THINK_RE = re.compile(r'<think>.*?</think>\s*', re.DOTALL)

def strip_thinking(text):
    """Remove Nemotron reasoning traces (<think>...</think>) from model output."""
    return _THINK_RE.sub('', text).strip()


def get_vllm_llm(_model=None):
    """Get a ChatOpenAI instance connected to the local Nemotron-3 Nano vLLM server."""
    llm = ChatOpenAI(
        base_url=VLLM_BASE_URL,
        model=MODEL_ID,
        temperature=0.1,
        top_p=0.9,
        max_tokens=8192,
        api_key="not-needed",
        extra_body={"chat_template_kwargs": {"enable_thinking": True}},
    )

    return llm


CODE_EXTENSIONS = (
    '*.py', '*.js', '*.ts', '*.jsx', '*.tsx',
    '*.java', '*.go', '*.rs', '*.rb', '*.php',
    '*.cs', '*.razor', '*.cpp', '*.c', '*.h',
    '*.json', '*.yaml', '*.yml', '*.toml',
    '*.sql', '*.sh', '*.bash', '*.ps1', '*.bat',
    '*.css', '*.html', '*.xml',
    '*.csproj', '*.sln', '*.gradle', '*.tf',
    '*.dockerfile', '*.proto',
)

DOC_EXTENSIONS = ('*.txt', '*.pdf', '*.md', '*.rst')

ALL_EXTENSIONS = DOC_EXTENSIONS + CODE_EXTENSIONS


def _collect_kb_files(folder_path):
    """Collect all knowledge-base files, recursing into nested subdirectories."""
    seen = set()
    files = []
    for pattern in ALL_EXTENSIONS:
        for f in folder_path.rglob(pattern):
            if f not in seen:
                seen.add(f)
                files.append(f)
    return files


def _get_kb_max_mtime(folder_path):
    """Return the latest modification time across all knowledge-base files."""
    max_mtime = 0
    for f in _collect_kb_files(folder_path):
        max_mtime = max(max_mtime, f.stat().st_mtime)
    return max_mtime


def _build_knowledge_base(folder_path):
    """Load documents, chunk them, and build a FAISS index."""
    docs = []

    for f in _collect_kb_files(folder_path):
        if f.suffix == '.pdf':
            loader = PyPDFLoader(str(f))
        else:
            loader = TextLoader(str(f), autodetect_encoding=True)
        docs.extend(loader.load())

    if not docs:
        return None

    text_splitter = RecursiveCharacterTextSplitter(
        separators=[
            "\nclass ",        # Python / Java / C# class definitions
            "\ndef ",          # Python function definitions
            "\nasync def ",    # Python async functions
            "\nfunction ",     # JS function definitions
            "\nexport ",       # JS/TS module exports
            "\nfunc ",         # Go function definitions
            "\nCREATE ",       # SQL DDL statements
            "\n## ",           # Markdown H2 headers
            "\n### ",          # Markdown H3 headers
            "\n\n",            # paragraph breaks
            "\n",              # line breaks (last resort)
        ],
        chunk_size=2000,
        chunk_overlap=400,
    )
    text_chunks = text_splitter.split_documents(docs)
    return FAISS.from_documents(text_chunks, embeddings)


def _kb_relative_path(source: str, kb_root: Path) -> str:
    """Path under knowledge-base for labels; basename if outside kb_root."""
    if not source or source == "Unknown":
        return "Unknown"
    try:
        p = Path(source).resolve()
        root = kb_root.resolve()
        rel = p.relative_to(root)
        return rel.as_posix()
    except (ValueError, OSError, RuntimeError):
        return Path(source).name



def vllm_rag_inference(model, query):
    """Process knowledge-base files (.razor, .cs, .ts, .js, .json, .sql, .css, etc.) using local vLLM deployment.
    
    Args:
        model (str): The user-friendly model name
        query (str): The user's question
    
    Returns:
        dict: The model's response and performance metrics
    """
    global _cached_knowledge_base, _cached_kb_mtime

    llm = get_vllm_llm()
    folder_path = Path(f"{working_dir}/knowledge-base")

    # Rebuild index only when files change
    current_mtime = _get_kb_max_mtime(folder_path)
    if _cached_knowledge_base is None or current_mtime != _cached_kb_mtime:
        os.write(1, b"Building FAISS index (first query or files changed)...\n")
        _cached_knowledge_base = _build_knowledge_base(folder_path)
        _cached_kb_mtime = current_mtime

    knowledge_base = _cached_knowledge_base
    if knowledge_base is None:
        return "No documents found in knowledge base."

    retriever = knowledge_base.as_retriever(search_kwargs={"k": 35}) # top k 
    retrieved_docs = retriever.invoke(query)
    
    # Store retrieved docs globally for source display
    global _last_retrieved_docs
    _last_retrieved_docs = retrieved_docs
    
    # Debug output
    os.write(1, f"Retrieved {len(retrieved_docs)} documents\n".encode())
    if retrieved_docs:
        os.write(1, f"First doc preview: {retrieved_docs[0].page_content[:200]}...\n".encode())
    
    # Labels: KB-relative path + part k/n when the same file appears in multiple chunks
    rel_paths = [
        _kb_relative_path(doc.metadata.get("source", "Unknown"), folder_path)
        for doc in retrieved_docs
    ]
    per_file_total = Counter(rel_paths)
    per_file_seen = {}
    context_parts = []
    for i, doc in enumerate(retrieved_docs, 1):
        rel = rel_paths[i - 1]
        per_file_seen[rel] = per_file_seen.get(rel, 0) + 1
        part_n = per_file_seen[rel]
        total = per_file_total[rel]
        part_suffix = f" — part {part_n}/{total}" if total > 1 else ""
        context_parts.append(
            f"[Excerpt {i}: `{rel}`{part_suffix}]\n{doc.page_content}"
        )
    unique_paths = sorted({p for p in rel_paths if p != "Unknown"})
    files_preamble = (
        "**Files in this context (only these paths were retrieved for this question):**\n"
        + "\n".join(f"- `{p}`" for p in unique_paths)
        if unique_paths
        else "**Files in this context:** _(no labeled paths)_"
    )
    context_text = (
        files_preamble + "\n\n---\n\n" + "\n\n---\n\n".join(context_parts)
    )
    
    system_prompt = (
        "You are a senior software engineer and coding assistant with deep expertise "
        "across multiple languages and frameworks. You help developers understand, "
        "refactor, extend, and debug codebases by grounding every answer in the "
        "source code excerpts provided below.\n\n"
        "Rules:\n"
        "- Base every claim on the excerpts below — names, signatures, and dependencies "
        "as shown. Do not invent APIs; if unsure, insert a TODO stub with explanation.\n"
        "- Follow the patterns visible in the excerpts (architecture, naming, dependency "
        "injection, error handling). Match the existing style exactly.\n"
        "- When reasoning about refactoring or ordering of work, point to specific call "
        "sites in the excerpts.\n"
        "- Code: idiomatic, with required imports; when the user wants implementation "
        "detail, show new pieces and how they integrate with the existing codebase.\n"
        "- Cite with backtick paths from excerpt headers; never excerpt numbers alone. "
        "Only attribute behavior to files listed under \"Files in this context\"."
    )

    user_prompt = (
        f"=== REFERENCE DOCUMENTATION ===\n"
        f"The following excerpts are from the application source code, docs, and any "
        f"patterns or templates in the knowledge base. Base your answer on these. \n\n "
        f"Do not make any assumptions\n\n"
        f"{context_text}\n\n"
        f"=== END REFERENCE DOCUMENTATION ===\n\n"
        f"Request: {query}\n\n"
        f"Instructions:\n"
        f"- Begin with one short line: **Sources consulted:** then a comma-separated list of "
        f"the `path` values you use (subset of the list above).\n"
        f"- Cite specific classes, methods, and paths from the documentation; use the same "
        f"`path` strings as in the excerpt headers.\n"
        f"- When generating code, follow the exact patterns visible in the excerpts "
        f"- If any interface, API, or behavior is unclear DO NOT invent it Insert a TODO with explanation" 
        f"- Provide a dedicated section listing all assumptions"
        f"- Provide a detailed, actionable answer with step by step instructions:"
    )

    messages = [SystemMessage(content=system_prompt), HumanMessage(content=user_prompt)]
    
    start_time = time.time()
    response = llm.invoke(messages)
    end_time = time.time()
    elapsed_time = end_time - start_time
    
    # Get token count from response metadata
    completion_tokens = None
    if hasattr(response, 'response_metadata'):
        completion_tokens = response.response_metadata.get('usage', {}).get('completion_tokens', None)
    
    # Calculate TPS
    if completion_tokens and elapsed_time > 0:
        tokens_per_second = completion_tokens / elapsed_time
        estimated_tokens = completion_tokens
    else:
        # Fallback: estimate from character count
        estimated_tokens = len(response.content) // 4
        tokens_per_second = estimated_tokens / elapsed_time if elapsed_time > 0 else 0
    
    # Strip any reasoning traces and append sources
    response_content = strip_thinking(response.content)
    
    if retrieved_docs:
        sources_text = "\n\n---\n\n**📚 Sources:**\n\n"
        unique_sources = set()
        for doc in retrieved_docs:
            rel = _kb_relative_path(doc.metadata.get("source", "Unknown"), folder_path)
            if rel != "Unknown":
                unique_sources.add(rel)

        if unique_sources:
            for rel in sorted(unique_sources):
                sources_text += f"• `{rel}`\n"
            response_content = response_content + sources_text
    
    # Return both response AND metrics
    return {
        'response': response_content,
        'metrics': {
            'tokens': estimated_tokens,
            'time': elapsed_time,
            'tps': tokens_per_second
        }
    }


def vllm_llm_inference(model, query):
    """Make a raw LLM call to the local vLLM server.
    
    Args:
        model (str): The user-friendly model name
        query (str): The user's question
    """
    llm = get_vllm_llm()
    
    messages = [
        SystemMessage(content="You are a helpful coding assistant. Provide detailed technical answers with code examples."),
        HumanMessage(content=query),
    ]
    
    start_time = time.time()
    response = llm.invoke(messages)
    end_time = time.time()
    elapsed_time = end_time - start_time
    
    # Get token count from response metadata
    completion_tokens = None
    if hasattr(response, 'response_metadata'):
        completion_tokens = response.response_metadata.get('usage', {}).get('completion_tokens', None)
    
    # Calculate TPS
    if completion_tokens and elapsed_time > 0:
        tokens_per_second = completion_tokens / elapsed_time
        estimated_tokens = completion_tokens
    else:
        # Fallback: estimate from character count
        estimated_tokens = len(response.content) // 4
        tokens_per_second = estimated_tokens / elapsed_time if elapsed_time > 0 else 0
    
    # Return both response AND metrics
    return {
        'response': strip_thinking(response.content),
        'metrics': {
            'tokens': estimated_tokens,
            'time': elapsed_time,
            'tps': tokens_per_second
        }
    }


def get_retrieved_sources():
    """Get the file sources from the last RAG query.
    
    Returns:
        list: List of tuples (kb_relative_path, preview) per retrieved chunk
    """
    global _last_retrieved_docs
    sources = []
    kb_root = Path(f"{working_dir}/knowledge-base")
    for doc in _last_retrieved_docs:
        source = doc.metadata.get("source", "Unknown")
        display_path = _kb_relative_path(source, kb_root)
        preview = doc.page_content[:150] + "..." if len(doc.page_content) > 150 else doc.page_content
        sources.append((display_path, preview))
    return sources


def clear_retrieved_sources():
    """Clear the retrieved sources (used when RAG is disabled)."""
    global _last_retrieved_docs
    _last_retrieved_docs = []
