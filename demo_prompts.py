"""
Demo prompts for CodeLlama 70B + RAG + C++ Engineering Workflows demo
90-Minute Demo Script
"""

DEMO_PROMPTS = {
    "1_intro": {
        "section": "1. Introduction to the Model & RAG (5 minutes)",
        "title": "Warm-up Prompt",
        "prompt": "Summarize in 5 bullet points how a modern LLM can assist C++ software engineers in a typical development workflow.",
        "rag_enabled": False,
        "description": "Short warm-up to introduce the model capabilities"
    },
    "2a_without_rag": {
        "section": "2. RAG vs Non-RAG Comparison (10 minutes)",
        "title": "A. Without RAG - Memory Management",
        "prompt": "Explain our company's Northwind Robotics recommended memory management strategy in C++.",
        "rag_enabled": False,
        "description": "Turn off RAG to show generic answer"
    },
    "2b_with_rag": {
        "section": "2. RAG vs Non-RAG Comparison (10 minutes)",
        "title": "B. With RAG - Memory Management",
        "prompt": "Explain our company's Northwind Robotics recommended memory management strategy in C++.",
        "rag_enabled": True,
        "description": "Enable RAG to show context-specific answer"
    },
    "3_class_design": {
        "section": "3. Generating C++ Code From Natural Language (10 minutes)",
        "title": "Prompt #3 — Class Design",
        "prompt": """Write a C++20 class ThreadPool with the following capabilities:

- Configurable number of worker threads
- A thread-safe task queue
- submit(std::function<void()>)
- Graceful shutdown on destructor

Please include headers, comments, and RAII principles.""",
        "rag_enabled": False,
        "description": "Generate a complete ThreadPool class with RAII"
    },
    "4_unit_tests": {
        "section": "3. Generating C++ Code From Natural Language (10 minutes)",
        "title": "Prompt #4 — Add Unit Tests",
        "prompt": """Generate GoogleTest unit tests for this ThreadPool class. Include tests for:

- Task execution
- Parallelism
- Shutdown behavior
- Stress test with 1,000 tasks

Keep tests minimal but illustrative.""",
        "rag_enabled": False,
        "description": "Generate comprehensive unit tests"
    },
    "5_api_diagram": {
        "section": "4. RAG for API/Codebase Navigation (10 minutes)",
        "title": "Prompt #5 — Using Your Project Docs",
        "prompt": "Based on our internal API docs, generate a UML-style text diagram illustrating how the DataProcessor, AsyncScheduler, and MetricsSink components interact.",
        "rag_enabled": True,
        "description": "Uses Northwind RAG dataset to generate architecture diagram"
    },
    "6_refactor": {
        "section": "4. RAG for API/Codebase Navigation (10 minutes)",
        "title": "Prompt #6 — Refactor using RAG knowledge",
        "prompt": "Using the documented patterns in our C++ Design Principles guide, refactor the earlier ThreadPool to follow our recommended concurrency and error handling conventions.",
        "rag_enabled": True,
        "description": "Refactor code using internal design principles"
    },
    "7_debug": {
        "section": "5. Debugging & Code Explanation (10 minutes)",
        "title": "Prompt #7 — Diagnose a bug",
        "prompt": """This code crashes or prints incorrect values. Explain why and fix it.

```cpp
std::vector<int*> v;

for (int i = 0; i < 10; i++) {
    int x = i * 10;
    v.push_back(&x);
}

std::cout << *v[5] << "\n";
```""",
        "rag_enabled": False,
        "description": "Debug a common C++ memory issue"
    },
    "8_optimize": {
        "section": "5. Debugging & Code Explanation (10 minutes)",
        "title": "Prompt #8 — Performance optimization",
        "prompt": """Optimize the following function for speed, safety, and clarity. List the issues with the original code, then provide a revised version and explain the improvements.

```cpp
int slowSum(const std::vector<int>& values) {
    int* temp = new int(0);
    for (size_t i = 0; i < values.size(); i++) {
        *temp = *temp + values[i];
    }
    int result = *temp;
    delete temp;
    return result;
}
```""",
        "rag_enabled": False,
        "description": "Optimize code and explain improvements"
    },
    "8_optimize_extra": {
        "section": "5. Debugging & Code Explanation (10 minutes)",
        "title": "Prompt #8 — Additional Optimization Example",
        "prompt": """Optimize the following function for speed, safety, and clarity. List the issues with the original code, then provide a revised version and explain the improvements.

```cpp
std::vector<int> transform(const std::vector<int>& data) {
    std::vector<int> result;
    for (int x : data) {
        result.push_back(x);
    }
    for (int i = 0; i < result.size(); i++) {
        result[i] = result[i] * 2;
    }
    return result;
}
```""",
        "rag_enabled": False,
        "description": "Additional optimization example"
    },
    "9_feature_plan": {
        "section": "6. Large-Scale Code Generation Workflow (10 minutes)",
        "title": "Prompt #9 — Feature Spec → Implementation Plan",
        "prompt": "I need to implement a plugin system for a C++ application. Write a 10-step implementation plan with architecture, class design, and loading mechanism (e.g., std::filesystem + dlopen / platform abstractions).",
        "rag_enabled": False,
        "description": "Generate implementation plan for plugin system"
    },
    "10_generate_modules": {
        "section": "6. Large-Scale Code Generation Workflow (10 minutes)",
        "title": "Prompt #10 — Generate Modules",
        "prompt": "Write the core plugin manager implementation based on step 3–6 of your plan. Include error handling and cross-platform abstractions.",
        "rag_enabled": False,
        "description": "Generate core implementation code"
    },
    "11_documentation": {
        "section": "6. Large-Scale Code Generation Workflow (10 minutes)",
        "title": "Prompt #11 — Document the Library",
        "prompt": "Generate Doxygen-ready documentation for the plugin manager.",
        "rag_enabled": False,
        "description": "Generate API documentation"
    },
    "qa_cpp23": {
        "section": "7. Interactive Q&A With the Customer (5 minutes)",
        "title": "Q&A: Port to C++23 Modules",
        "prompt": "Can you port this code to C++23 modules?",
        "rag_enabled": False,
        "description": "Interactive Q&A example"
    },
    "qa_cmake": {
        "section": "7. Interactive Q&A With the Customer (5 minutes)",
        "title": "Q&A: Generate CMake Build System",
        "prompt": "Can you generate a build system using CMake?",
        "rag_enabled": False,
        "description": "Interactive Q&A example"
    },
    "qa_stack_trace": {
        "section": "7. Interactive Q&A With the Customer (5 minutes)",
        "title": "Q&A: Explain Stack Trace",
        "prompt": "Can you explain this stack trace?",
        "rag_enabled": False,
        "description": "Interactive Q&A example"
    },
    "qa_simd": {
        "section": "7. Interactive Q&A With the Customer (5 minutes)",
        "title": "Q&A: SIMD Version",
        "prompt": "Can you write a SIMD version using SSE/AVX?",
        "rag_enabled": False,
        "description": "Interactive Q&A example"
    }
}

