import gradio as gr
from gradio_toggle import Toggle  # pyright: ignore[reportMissingImports]
import time

from local_code_assistant_engine import (
    vllm_rag_inference, 
    vllm_llm_inference, 
    get_retrieved_sources,
    clear_retrieved_sources
)


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
            # Clear retrieved sources when RAG is disabled
            clear_retrieved_sources()
            result = vllm_llm_inference(selected_model, message)
        
        # Extract response text and metrics
        full_response = result['response']
        metrics = result['metrics']
        
        # Stream the response character by character for visual effect
        for i in range(len(full_response)):
            time.sleep(0.0001)
            yield full_response[:i+1]
        
        # AFTER streaming completes, append metrics
        metrics_text = f"\n\n---\n\n⚡ **Performance:** {metrics['tps']:.1f} tokens/sec | {metrics['tokens']} tokens in {metrics['time']:.2f}s"
        yield full_response + metrics_text
        
    except Exception as e:
        yield f"Error: {str(e)}"


# Create the demo
with gr.Blocks(title="AI Coding Assistant", fill_height=True) as demo:
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
        #chatbot-container .chatbot {
            min-height: 600px !important;
            height: auto !important;
            max-height: none !important;
            overflow: visible !important;
        }
        .controls-row {
            align-items: end !important;
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
    
    # Model display and RAG toggle
    with gr.Row(elem_classes=["controls-row"]):
        model_name = gr.Textbox(
            value="Nemotron-3 Nano 30B",
            label="Model",
            interactive=False,
        )
        
        # Toggle switch to choose between RAG and non‑RAG modes
        rag_toggle = Toggle(
            label="Use Knowledge Base",
            value=True,
            interactive=True,
        )
    
    # ChatInterface
    with gr.Column(elem_id="chatbot-container"):
        chat_interface = gr.ChatInterface(
            fn=coding_assistant_streaming,
            additional_inputs=[rag_toggle, model_name],
            title=None,
            type="messages",
            chatbot=gr.Chatbot(height=None, type="messages", allow_tags=False, show_copy_button=True),
        )

    # Display retrieved sources (only shown when RAG is enabled)
    with gr.Accordion("📚 Retrieved Sources", open=False):
        sources_display = gr.Markdown("No sources retrieved yet. Ask a question with RAG enabled to see retrieved code and docs.")
        
        def update_sources_display(rag_enabled):
            """Update the sources display with the last retrieved documents.
            
            Args:
                rag_enabled: Boolean indicating if RAG is currently enabled
            """
            if not rag_enabled:
                return "⚠️ **RAG mode is currently disabled.** Enable 'Use Knowledge Base' to see retrieved sources."
            
            sources = get_retrieved_sources()
            if not sources:
                return "No sources retrieved yet. Ask a question with RAG enabled to see retrieved code and docs."
            
            markdown = "### Source Files:\n\n"
            for i, (path, preview) in enumerate(sources, 1):
                markdown += f"**{i}. `{path}`**\n\n"
                markdown += f"```\n{preview}\n```\n\n"
                markdown += "---\n\n"
            return markdown
        
        refresh_sources_btn = gr.Button("🔄 Refresh Sources 🔄", variant="secondary")
        refresh_sources_btn.click(
            fn=update_sources_display,
            inputs=[rag_toggle],
            outputs=[sources_display]
        )


if __name__ == "__main__":
    demo.launch(
        share=False,
        server_name="0.0.0.0",
        server_port=7860,
        show_error=True
        )

