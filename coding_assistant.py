import gradio as gr
from gradio_toggle import Toggle
import time

from local_code_assistant_engine import (
    vllm_rag_inference, 
    vllm_llm_inference, 
    get_retrieved_sources
)
from demo_prompts import DEMO_PROMPTS


def coding_assistant_streaming(message, history, rag_toggle, selected_model):
    """
    Coding assistant with streaming response - yields response character by character.
    This function is used by ChatInterface for streaming responses.
    
    Args:
        message: User's message
        history: Chat history (list of [user_message, assistant_message] tuples)
        rag_toggle: Boolean indicating whether to use RAG
        selected_model: Selected model name from dropdown
    """
    try:
        # Get the full response (now a dict with 'response' and 'metrics')
        if rag_toggle:
            result = vllm_rag_inference(selected_model, message)
        else:
            result = vllm_llm_inference(selected_model, message)
        
        # Extract response text and metrics
        full_response = result['response']
        metrics = result['metrics']
        
        # Stream the response character by character for visual effect
        for i in range(len(full_response)):
            time.sleep(0.01)
            yield full_response[:i+1]
        
        # AFTER streaming completes, append metrics
        metrics_text = f"\n\n---\n\n⚡ **Performance:** {metrics['tps']:.1f} tokens/sec | {metrics['tokens']} tokens in {metrics['time']:.2f}s"
        yield full_response + metrics_text
        
    except Exception as e:
        yield f"Error: {str(e)}"


# Create the demo
with gr.Blocks(title="AI Coding Assistant") as demo:
    # Custom CSS 
    gr.HTML(
        """
        <style>
        .large-text h1 {
            font-size: 2.5em !important;
            margin: 0 !important;
            line-height: 1.2 !important;
            padding-top: 20px !important;
            text-align: center !important;
        }
        </style>
        """
    )

    # Header row with title
    with gr.Row(elem_classes=["compact-header"]):
        gr.Markdown(
            "# AI Coding Assistant",
            elem_classes=["large-text"],
        )
            
    # RAG diagram on top
    gr.Image("rag_diagram.png", show_label=False)
    
    # Model selection and RAG toggle
    with gr.Row():
        model_dropdown = gr.Dropdown(
            choices=[
                "CodeLlama 70B Instruct",
                "CodeLlama 70B",
            ],
            value="CodeLlama 70B Instruct",
            label="Select Model",
            info="Choose the LLM model (ensure the vLLM service is running)",
        )
        
        # Toggle switch to choose between RAG and non‑RAG modes
        rag_toggle = Toggle(
            label="Use Knowledge Base",
            value=True,
            interactive=True,
        )
    
    # Demo Prompts Panel - Easy copy-paste access
    with gr.Accordion("📋 Demo Prompts", open=False):
        
        # Use prompts in the order they appear in demo_prompts.py
        demo_prompt_dropdown = gr.Dropdown(
            choices=[(v['title'], k) for k, v in DEMO_PROMPTS.items()],
            label="Select Demo Prompt",
        )
        demo_prompt_display = gr.Markdown()
        
        def update_prompt_display(prompt_key):
            """Update the prompt display when a prompt is selected."""
            if not prompt_key or prompt_key not in DEMO_PROMPTS:
                return ""
            
            prompt_data = DEMO_PROMPTS[prompt_key]
            prompt_text = prompt_data['prompt']
            # Format as markdown code block to get automatic copy button like in Chatbot
            formatted_prompt = f"```\n{prompt_text}\n```"
            return formatted_prompt
        
        demo_prompt_dropdown.change(
            fn=update_prompt_display,
            inputs=[demo_prompt_dropdown],
            outputs=[demo_prompt_display]
        )
        
        gr.Markdown("💡 **Tip:** The prompt is displayed above - just hit the copy button to paste into chat.")
    
    # ChatInterface with streaming echo functionality
    chat_interface = gr.ChatInterface(
        fn=coding_assistant_streaming,
        additional_inputs=[rag_toggle, model_dropdown],
        title=None,
    )
    
    # Display retrieved sources (only shown when RAG is enabled)
    with gr.Accordion("📚 Retrieved Sources", open=False):
        sources_display = gr.Markdown("No sources retrieved yet. Ask a question with RAG enabled to see source files.")
        
        def update_sources_display():
            """Update the sources display with the last retrieved documents."""
            sources = get_retrieved_sources()
            if not sources:
                return "No sources retrieved yet. Ask a question with RAG enabled to see source files."
            
            markdown = "### Source Files:\n\n"
            for i, (filename, preview) in enumerate(sources, 1):
                markdown += f"**{i}. {filename}**\n\n"
                markdown += f"```\n{preview}\n```\n\n"
                markdown += "---\n\n"
            return markdown
        
        refresh_sources_btn = gr.Button("🔄 Refresh Sources 🔄", variant="secondary")
        refresh_sources_btn.click(
            fn=update_sources_display,
            outputs=[sources_display]
        )


if __name__ == "__main__":
    demo.launch(
        share=False,
        server_name="0.0.0.0",
        server_port=7860,
        show_error=True
        )

