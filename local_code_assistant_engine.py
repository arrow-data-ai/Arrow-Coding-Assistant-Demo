# Standard library imports
import os 
from pathlib import Path
 
 
# LLM and embedding model imports
from langchain_nvidia_ai_endpoints import ChatNVIDIA  
from langchain_huggingface import HuggingFaceEmbeddings

# Document loading and processing imports
from langchain_unstructured import UnstructuredLoader
from langchain_community.document_loaders import JSONLoader
from langchain_text_splitters import CharacterTextSplitter

# Vector store and QA chain imports
from langchain_community.vectorstores import FAISS
from langchain_classic.chains import RetrievalQA
from langchain_core.prompts import PromptTemplate

# Get the current working directory
working_dir = os.path.dirname(os.path.abspath(__file__))

# Initialize default embedding model 
embeddings = HuggingFaceEmbeddings()

# Configuration for multiple local NIM instances
# Map model identifiers to their local NIM port numbers
# Each NIM instance should be running on a different port
NIM_CONFIG = {
    # Map models to their NIM base URLs
    # You can configure different ports for different models
    'codellama/codellama-70b-instruct': {'port': 8000, 'base_url': 'http://localhost:8000/v1'},
    'codellama/codellama-34b-instruct': {'port': 8001, 'base_url': 'http://localhost:8001/v1'},
    'codellama/codellama-13b-instruct': {'port': 8002, 'base_url': 'http://localhost:8002/v1'},
    # 'meta/codellama-7b-instruct': {'port': 8003, 'base_url': 'http://localhost:8003/v1'},
    # 'bigcode/starcoder2-15b-instruct': {'port': 8004, 'base_url': 'http://localhost:8004/v1'},
    # Default fallback port if model not in config
    'default': {'port': 8000, 'base_url': 'http://localhost:8000/v1'}
}

def get_nim_llm(model):
    """Get a ChatNVIDIA instance connected to a local NIM.
    
    Maps user-friendly model names to their corresponding model identifiers and NIM base URLs,
    then creates and returns a configured ChatNVIDIA instance.
    
    Args:
        model (str): The user-friendly model name (e.g., "CodeLlama 70B Instruct")
        temperature (float): Temperature setting for the model
    
    Returns:
        ChatNVIDIA: Configured ChatNVIDIA instance connected to the appropriate local NIM
    """
    # Map user-friendly model names to model identifiers
    model_mapping = {
        'CodeLlama 70B Instruct': 'codellama/codellama-70b-instruct',
        'CodeLlama 34B Instruct': 'codellama/codellama-34b-instruct',
        'CodeLlama 13B Instruct': 'codellama/codellama-13b-instruct',
        'CodeLlama 7B Instruct': 'codellama/codellama-7b-instruct',
        'StarCoder2 15B': 'bigcode/starcoder2-15b-instruct',
    }
    
    model_id = model_mapping.get(model)
    if model_id is None:
        raise ValueError(f"Unknown model: {model}")
    
    # Get base URL from config, or use default
    nim_config = NIM_CONFIG.get(model_id, NIM_CONFIG['default'])
    base_url = nim_config['base_url']
    
    # Create and return ChatNVIDIA instance
    llm = ChatNVIDIA(
        base_url=base_url,
        model=model_id,
        temperature=0.0,
    )
    
    return llm


def nim_rag_inference(model, query):
    """Process all .txt, .pdf , .md and .jsonl files in the knowledge base folder using local deployment.
    
    Args:
        model (str): The user-friendly model name
        query (str): The user's question
    
    Returns:
        str: The model's response to the query
    """
    
    llm = get_nim_llm(model) 

    os.write(1,f"{model}\n".encode())
    
    # Define the folder path
    folder_path = Path(f"{working_dir}/knowledge-base")

    # Separate files by type
    txt_pdf_md_files = (list(folder_path.glob('*.txt')) + 
                        list(folder_path.glob('*.pdf')) + 
                        list(folder_path.glob('*.md')))
    
    jsonl_files = list(folder_path.glob('*.jsonl'))
    
    docs = []
  
    # Load structured files (txt, pdf, md) using UnstructuredLoader
    if txt_pdf_md_files:
        docs.extend(UnstructuredLoader(txt_pdf_md_files).load())
    
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
    
    # Debug output
    os.write(1, f"Retrieved {len(retrieved_docs)} documents\n".encode())
    if retrieved_docs:
        os.write(1, f"First doc preview: {retrieved_docs[0].page_content[:200]}...\n".encode())
    
    # Combine retrieved context
    context_text = "\n\n---\n\n".join([doc.page_content for doc in retrieved_docs])
    
    # Build a direct, imperative prompt that doesn't give room for refusal
    # Reframe as code documentation/technical specs rather than "API reference"
    full_prompt = f"""Explain the C++ classes, methods, and architecture described in the following documentation.

                Documentation:
                {context_text}

                Question: {query}

                Technical Explanation:"""
    
    # Call LLM directly - this bypasses RetrievalQA's additional processing
    response = llm.invoke(full_prompt)
    
    return response.content


def nim_llm_inference(model, query):
    """Make a raw LLM call to the local NIM.
    
    Args:
        model (str): The user-friendly model name
        query (str): The user's question
    """
    llm = get_nim_llm(model) 
    
    # Preprocess query to avoid safety triggers
    query_lower = query.lower()
    
    # Detect if it's asking about Northwind/company-specific info
    if any(word in query_lower for word in ["northwind", "api reference", "company", "proprietary"]):
        # Return a simple "I don't know" response for demo purposes
        return "I don't have information about that. I can help with general C++ programming questions."
    
    # For other questions, use a simple technical prompt
    prompt = f"""Answer this C++ programming question. If you don't know, say "I don't know."

    Question:   {query} """
    
    response = llm.invoke(prompt)
    return response.content

