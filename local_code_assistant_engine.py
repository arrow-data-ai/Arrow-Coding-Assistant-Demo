# Standard library imports
import os 
import re
from pathlib import Path

# LLM and embedding model imports
from langchain_openai import ChatOpenAI
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_core.messages import SystemMessage, HumanMessage

# Document loading and processing imports
from langchain_community.document_loaders import JSONLoader, TextLoader, PyPDFLoader
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

MODEL_ID = 'nvidia/NVIDIA-Nemotron-3-Nano-30B-A3B-BF16'
VLLM_BASE_URL = 'http://localhost:8000/v1'

_THINK_RE = re.compile(r'<think>.*?</think>\s*', re.DOTALL)

def strip_thinking(text):
    """Remove Nemotron reasoning traces (<think>...</think>) from model output."""
    return _THINK_RE.sub('', text).strip()

def get_vllm_llm(_model=None):
    """Get a ChatOpenAI instance connected to the local Nemotron-3 Nano vLLM server."""
    llm = ChatOpenAI(
        base_url=VLLM_BASE_URL,
        model=MODEL_ID,
        temperature=0.4,
        top_p=0.9,
        max_tokens=8192,
        api_key="not-needed",
        extra_body={"chat_template_kwargs": {"enable_thinking": True}},
    )
    
    return llm


def _get_kb_max_mtime(folder_path):
    """Return the latest modification time across all knowledge-base files."""
    max_mtime = 0
    for pattern in ('*.txt', '*.pdf', '*.md', '*.jsonl',
                     '*.cpp', '*.hpp', '*.h', '*.cc'):
        for f in folder_path.glob(pattern):
            max_mtime = max(max_mtime, f.stat().st_mtime)
    return max_mtime


def _build_knowledge_base(folder_path):
    """Load documents, chunk them, and build a FAISS index."""
    txt_files = list(folder_path.glob('*.txt'))
    pdf_files = list(folder_path.glob('*.pdf'))
    md_files = list(folder_path.glob('*.md'))
    jsonl_files = list(folder_path.glob('*.jsonl'))
    cpp_files = [f for ext in ('*.cpp', '*.hpp', '*.h', '*.cc')
                 for f in folder_path.glob(ext)]

    docs = []

    for txt_file in txt_files:
        loader = TextLoader(str(txt_file))
        docs.extend(loader.load())

    for pdf_file in pdf_files:
        loader = PyPDFLoader(str(pdf_file))
        docs.extend(loader.load())

    for md_file in md_files:
        loader = TextLoader(str(md_file))
        docs.extend(loader.load())

    for jsonl_file in jsonl_files:
        loader = JSONLoader(
            file_path=str(jsonl_file),
            jq_schema=".text",
            text_content=False,
            json_lines=True,
        )
        docs.extend(loader.load())

    for cpp_file in cpp_files:
        loader = TextLoader(str(cpp_file))
        docs.extend(loader.load())

    if not docs:
        return None

    text_splitter = RecursiveCharacterTextSplitter(
        separators=[
            "\n// ===",       # ShopCore section headers (class boundaries)
            "\nclass ",       # C++ class definitions
            "\n## ",          # Markdown H2 headers
            "\n### ",         # Markdown H3 headers
            "\n\n",           # paragraph breaks
            "\n",             # line breaks (last resort)
        ],
        chunk_size=3000,
        chunk_overlap=500,
    )
    text_chunks = text_splitter.split_documents(docs)
    return FAISS.from_documents(text_chunks, embeddings)


def vllm_rag_inference(model, query):
    """Process all .txt, .pdf, .md and .jsonl files in the knowledge base folder using local vLLM deployment.
    
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

    retriever = knowledge_base.as_retriever(search_kwargs={"k": 8})
    retrieved_docs = retriever.invoke(query)
    
    # Store retrieved docs globally for source display
    global _last_retrieved_docs
    _last_retrieved_docs = retrieved_docs
    
    # Debug output
    os.write(1, f"Retrieved {len(retrieved_docs)} documents\n".encode())
    if retrieved_docs:
        os.write(1, f"First doc preview: {retrieved_docs[0].page_content[:200]}...\n".encode())
    
    # Combine retrieved context with source labels so the model can cross-reference
    context_parts = []
    for i, doc in enumerate(retrieved_docs, 1):
        source = doc.metadata.get('source', 'Unknown')
        filename = os.path.basename(source) if source != 'Unknown' else 'Unknown'
        context_parts.append(
            f"[Document {i}: {filename}]\n{doc.page_content}"
        )
    context_text = "\n\n---\n\n".join(context_parts)
    
    system_prompt = (
        "You are a software architect specializing in monolith-to-microservices migration "
        "using the Strangler Fig pattern. You work with C++ codebases.\n\n"
        "CRITICAL RULES — follow these exactly:\n\n"
        "1. GROUND EVERY CLAIM in the reference documentation provided below. "
        "Use the exact class names, method signatures, data structures, and dependency "
        "relationships from the source code. Do NOT invent classes, namespaces, or API "
        "names that do not appear in the documentation.\n\n"
        "2. When the documentation provides code templates or patterns (e.g. interface "
        "definitions, facade patterns, DI setup), REPLICATE those patterns exactly rather "
        "than inventing alternatives. Adapt them to the specific service being extracted.\n\n"
        "3. When generating code:\n"
        "   - Produce compilable, modern C++20 code with all required #include headers\n"
        "   - Use pure abstract interfaces (IService) with domain-level signatures "
        "(plain C++ types), NOT transport-specific types like grpc::Status or protobuf "
        "messages in the interface\n"
        "   - Keep gRPC/protobuf details inside the RemoteService implementation only\n"
        "   - The facade must inherit from the abstract interface\n"
        "   - Include constructor-based dependency injection\n"
        "   - Show both the new microservice code AND the monolith modifications\n\n"
        "4. For dependency analysis and extraction ordering, cite the specific call sites "
        "from the monolith source code (e.g. 'OrderService::place_order calls "
        "NotificationService::send_email on line N').\n\n"
        "5. Do NOT fabricate library classes, gRPC helpers, or utility types that do not "
        "exist in standard C++, gRPC, or the provided codebase. If you are unsure whether "
        "a class exists, use a clearly marked stub with a TODO comment instead."
    )

    user_prompt = (
        f"=== REFERENCE DOCUMENTATION ===\n"
        f"The following excerpts are from the ShopCore monolith source code, "
        f"migration patterns, and microservice templates. Base your answer on these.\n\n"
        f"{context_text}\n\n"
        f"=== END REFERENCE DOCUMENTATION ===\n\n"
        f"Request: {query}\n\n"
        f"Instructions:\n"
        f"- Cite specific classes, methods, and line references from the documentation above.\n"
        f"- When generating code, follow the exact patterns shown in the template documents "
        f"(interface definitions, facade pattern, DI wiring).\n"
        f"- Ensure all code compiles: include all #include directives, use correct types, "
        f"and do not reference classes or methods that don't exist.\n"
        f"- Provide a detailed, actionable answer:"
    )

    messages = [SystemMessage(content=system_prompt), HumanMessage(content=user_prompt)]
    
    import time
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
            source = doc.metadata.get('source', 'Unknown')
            filename = os.path.basename(source) if source != 'Unknown' else 'Unknown'
            if filename != 'Unknown':
                unique_sources.add(filename)
        
        if unique_sources:
            for i, filename in enumerate(sorted(unique_sources), 1):
                sources_text += f"• `{filename}`\n"
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
        SystemMessage(content="You are a software architect specializing in monolith-to-microservices migration using the Strangler Fig pattern for C++ codebases. Provide detailed technical answers with code examples."),
        HumanMessage(content=query),
    ]
    
    import time
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
        list: List of tuples (filename, preview) for each retrieved document
    """
    global _last_retrieved_docs
    sources = []
    for doc in _last_retrieved_docs:
        # Get source from metadata, fallback to 'Unknown'
        source = doc.metadata.get('source', 'Unknown')
        # Extract just the filename from the full path
        filename = os.path.basename(source) if source != 'Unknown' else 'Unknown'
        # Get a preview of the content (first 150 characters)
        preview = doc.page_content[:150] + "..." if len(doc.page_content) > 150 else doc.page_content
        sources.append((filename, preview))
    return sources


def clear_retrieved_sources():
    """Clear the retrieved sources (used when RAG is disabled)."""
    global _last_retrieved_docs
    _last_retrieved_docs = []




