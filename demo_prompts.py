"""
Demo prompts for CodeLlama 70B + RAG + C++ Engineering Workflows demo
90-Minute Demo Script

This file contains all prompts from the demo script organized by section.
Each prompt can be easily copied and pasted into the chat interface.
"""

DEMO_PROMPTS = {
    "1_intro": {
        "title": "Prompt #1 — Introduction to the Model & RAG",
        "prompt": "Summarize in 5 bullet points how a modern LLM can assist C++ software engineers in a typical development workflow.",
        "description": "Short warm-up to introduce the model capabilities"
    },
    "2_with_without_rag": {
        "title": "Prompt #2 - Memory Management",
        "prompt": "Explain best C++ coding practices.",
        "description": "Turn off RAG to show generic answer"
    },
    "3_class_design": {
        "title": "Prompt #3 — Class Design",
        "prompt": """Write a C++20 class ThreadPool with the following capabilities:

        - Configurable number of worker threads
        - A thread-safe task queue
        - submit(std::function<void()>)
        - Graceful shutdown on destructor

        Please include headers, comments, and RAII principles.""",
        "description": "Generate a complete ThreadPool class with RAII"
    },
    "4_unit_tests": {
        "title": "Prompt #4 — Add Unit Tests",
        
        "prompt": """Generate GoogleTest unit tests for the following C++20 ThreadPool class.

                        Include tests for:

                        - Task execution
                        - Parallelism
                        - Shutdown behavior
                        - Stress test with 1,000 tasks

                        **Requirements:**

                        1. All tests must be deterministic — do **not** use `std::this_thread::sleep_for()` to wait for tasks. Use futures or other proper synchronization.

                        2. Do **not** manually call the destructor; let it run at scope exit.

                        3. For parallelism, verify that tasks run on multiple threads using thread IDs or other means.

                        4. Stress tests must safely wait for all tasks to complete before assertions.

                        5. Use modern C++20 features (std::jthread, std::atomic, std::invoke_result_t) where appropriate.

                        6. Keep tests minimal but illustrative.""",
                        "description": "Generate comprehensive unit tests"
            },
    "5_refactor": {
        "title": "Prompt #5 — Refactor using RAG knowledge",
        "prompt": "Using the documented patterns in our C++ Design Principles guide, refactor the earlier ThreadPool to follow our recommended concurrency and error handling conventions.",
        "description": "Refactor code using internal design principles"
    },
    "6_debug": {
        "title": "Prompt #6 — Diagnose a bug",
        "prompt": """This code crashes or prints incorrect values. Explain why and fix it.

        std::vector<int*> v;

        for (int i = 0; i < 10; i++) {
            int x = i * 10;
            v.push_back(&x);
        }

        std::cout << *v[5] << "\n";
        """,
        "description": "Debug a common C++ memory issue"
    },
    "7_optimize": {
        "title": "Prompt #7 — Performance optimization",
        "prompt": """Optimize the following function for speed, safety, and clarity. List the issues with the original code, then provide a revised version and explain the improvements.

        int slowSum(const std::vector<int>& values) {
            int* temp = new int(0);
            for (size_t i = 0; i < values.size(); i++) {
                *temp = *temp + values[i];
            }
            int result = *temp;
            delete temp;
            return result;
        }
        """,
        "description": "Optimize code and explain improvements"
    },
    "8_optimize_extra": {
        "title": "Prompt #8 — Additional Optimization Example",
        "prompt": """Optimize the following function for speed, safety, and clarity. List the issues with the original code, then provide a revised version and explain the improvements.

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
        """,
        "description": "Additional optimization example"
    },
    "9_feature_plan": {
        "title": "Prompt #9 — Feature Spec → Implementation Plan",
        "prompt": "I need to implement a plugin system for a C++ application. Write a 10-step implementation plan with architecture, class design, and loading mechanism (e.g., std::filesystem + dlopen / platform abstractions).",
        "description": "Generate implementation plan for plugin system"
    },
    "10_generate_modules": {
        "title": "Prompt #10 — Generate Modules",
        "prompt": """Task: Generate code with the following requirements:
                        1. Provide core classes:
                        - PluginManager
                        - IPlugin (base plugin interface)
                        - IPluginInterface (base plugin interface interface)
                        - Plugin (implements IPlugin)
                        - PluginInterface (implements IPluginInterface)
                        
                        2. Implement plugin loading:
                        - Platform-specific: dlopen/dlsym on Linux, LoadLibrary/GetProcAddress on Windows
                        - Include error handling for failed loads and missing symbols
                        
                        3. Implement plugin lifecycle:
                        - load, initialize, interact, unload
                        - Ensure RAII and safe resource cleanup
                        
                        4. Provide plugin discovery:
                        - Scan a directory for dynamic libraries
                        - Automatically load and register plugins
                        
                        5. Include interaction methods:
                        - Retrieve plugin info
                        - Call plugin methods
                        - Access plugin data
                        
                        6. Ensure clarity, safety, and modern C++20 practices:
                        - Use smart pointers (std::unique_ptr / std::shared_ptr) where needed
                        - Use RAII for resource management
                        - Provide comments for key methods
                        
                        Output:
                        - Fully compilable C++20 code with headers included
                        - Example usage in main() demonstrating plugin loading, interaction, and unloading""",
        "description": "Generate core implementation code"
    },
    "11_documentation": {
        "title": "Prompt #11 — Document the Library",
        "prompt": "Generate Doxygen-ready documentation for the plugin manager.",
        "description": "Generate API documentation"
    }
}

