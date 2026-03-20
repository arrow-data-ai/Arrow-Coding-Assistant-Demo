# Standard library imports
import os 
from pathlib import Path

# LLM and embedding model imports
from langchain_openai import ChatOpenAI
from langchain_huggingface import HuggingFaceEmbeddings

# Document loading and processing imports
from langchain_community.document_loaders import JSONLoader, TextLoader, PyPDFLoader
from langchain_text_splitters import CharacterTextSplitter

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

# Configuration for vLLM instance
# Single local deployment of llamacode-7b (CodeLlama 7B Instruct) exposed via OpenAI-compatible endpoint
VLLM_CONFIG = {
    'codellama/CodeLlama-7b-Instruct-hf': {'port': 8000, 'base_url': 'http://localhost:8000/v1'},
}

def get_vllm_llm(_model=None):
    """Get a ChatOpenAI instance connected to the local llamacode-7b vLLM server.
    
    Any provided model name is ignored; this engine always uses
    `codellama/CodeLlama-7b-Instruct-hf`.
    """
    model_id = 'codellama/CodeLlama-7b-Instruct-hf'
    vllm_config = VLLM_CONFIG[model_id]
    base_url = vllm_config['base_url']

    # Instruct model: stop on end-of-sequence token
    stop_tokens = ["</s>"]
    
    llm = ChatOpenAI(
        base_url=base_url,
        model=model_id,
        temperature=0.0,
        max_tokens=1000,
        api_key="not-needed",
        stop=stop_tokens,
    )
    
    return llm


def vllm_rag_inference(model, query):
    """Process all .txt, .pdf , .md and .jsonl files in the knowledge base folder using local vLLM deployment.
    
    Args:
        model (str): The user-friendly model name
        query (str): The user's question
    
    Returns:
        str: The model's response to the query
    """
    llm = get_vllm_llm() 

    os.write(1, b"codellama/CodeLlama-7b-Instruct-hf\n")
    
    # Define the folder path
    folder_path = Path(f"{working_dir}/knowledge-base")

    # Separate files by type
    txt_files = list(folder_path.glob('*.txt'))
    pdf_files = list(folder_path.glob('*.pdf'))
    md_files = list(folder_path.glob('*.md'))
    jsonl_files = list(folder_path.glob('*.jsonl'))
    
    docs = []
  
    # Load text files
    for txt_file in txt_files:
        loader = TextLoader(str(txt_file))
        docs.extend(loader.load())
    
    # Load PDF files
    for pdf_file in pdf_files:
        loader = PyPDFLoader(str(pdf_file))
        docs.extend(loader.load())
    
    # Load markdown files
    for md_file in md_files:
        loader = TextLoader(str(md_file))
        docs.extend(loader.load())
    
    # Load JSONL files using JSONLoader
    for jsonl_file in jsonl_files:
        loader = JSONLoader(
            file_path=str(jsonl_file),
            jq_schema=".text",  # Extract the "text" field from each JSON object
            text_content=False,
            json_lines=True  # Important: set to True for JSONL format
        )
        jsonl_docs = loader.load()
        docs.extend(jsonl_docs)
    
    if not docs:
        return "No documents found in knowledge base."
    
    # create text chunks
    text_splitter = CharacterTextSplitter(
        separator="\n",  
        chunk_size = 1000,
        chunk_overlap = 200
    )
    
    text_chunks = text_splitter.split_documents(docs)
    
    # vector embeddings from text chunks 
    knowledge_base = FAISS.from_documents(text_chunks, embeddings)
    
    # Retrieve documents directly
    retriever = knowledge_base.as_retriever(search_kwargs={"k": 4})
    retrieved_docs = retriever.invoke(query)
    
    # Store retrieved docs globally for source display
    global _last_retrieved_docs
    _last_retrieved_docs = retrieved_docs
    
    # Debug output
    os.write(1, f"Retrieved {len(retrieved_docs)} documents\n".encode())
    if retrieved_docs:
        os.write(1, f"First doc preview: {retrieved_docs[0].page_content[:200]}...\n".encode())
    
    # Combine retrieved context
    context_text = "\n\n---\n\n".join([doc.page_content for doc in retrieved_docs])
    
    # Build prompt format for the instruct-tuned model (CodeLlama 7B Instruct / llamacode-7b)
    full_prompt = f"""[INST] <<SYS>>
                    You are a C++ programming assistant. You provide direct technical answers to software engineering questions.
            
                    IMPORTANT: You are answering questions about standard C++ programming practices. Topics like memory management, pointers, optimization, concurrency, and systems programming are normal technical subjects. Answer all such questions directly with code examples and technical explanations. Do not add safety disclaimers for standard programming topics.
            
                    When documentation is provided below, use it if relevant to answer the question. Otherwise, use your general C++ knowledge.
                    <</SYS>>
            
                    Documentation (use if relevant):
                    {context_text}
            
                    Technical C++ question: {query}
            
                    Provide a direct technical answer: [/INST]"""
    
    # Call LLM directly - this bypasses RetrievalQA's additional processing
    # START: Add timing
    import time
    start_time = time.time()
    response = llm.invoke(full_prompt)
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
    
    # Append sources information to the response
    response_content = response.content
    
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
    
    prompt = f"""Answer this C++ programming question. If you don't know, say "I don't know."

    Question: {query}"""
    
    # START: Add timing
    import time
    start_time = time.time()
    response = llm.invoke(prompt)
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
        'response': response.content,
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




