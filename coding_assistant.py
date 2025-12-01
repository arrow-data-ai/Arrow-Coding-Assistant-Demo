import gradio as gr
from gradio_toggle import Toggle

from local_code_assistant_engine import nim_rag_inference, nim_llm_inference



def coding_assistant(message, history, rag_toggle, selected_model):
    """
    Coding assistant that answers questions and helps troubleshoot code using the RAG engine.
    
    Args:
        message: User's message
        history: Chat history
        rag_toggle: Boolean indicating whether to use RAG
        selected_model: Selected model name from dropdown
    """


    if rag_toggle:
        # RAG mode: query the Arrow knowledge base
        return nim_rag_inference(selected_model, message)

    # Non‑RAG mode placeholder
    
    else: 
        return nim_llm_inference(selected_model, message)

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
    
    # Model selection dropdown
    model_dropdown = gr.Dropdown(
        choices=[
            "CodeLlama 70B Instruct",
            "CodeLlama 34B Instruct",
            "CodeLlama 13B Instruct",
            # "CodeLlama 7B Instruct",
            # "StarCoder2 15B",
        ],
        value="CodeLlama 70B Instruct",
        label="Select Model",
        info="Choose the LLM model for the coding assistant",
    )
    
    # Toggle switch to choose between RAG and non‑RAG modes (using gradio-toggle)
    rag_toggle = Toggle(
        label="Use Knowledge Base",
        value=True,
        interactive=True,
    )

    # Chat interface wired through the router so it can switch modes
    chat_interface = gr.ChatInterface(
        fn=coding_assistant,
        additional_inputs=[rag_toggle, model_dropdown],
        autofocus=False,
    )


if __name__ == "__main__":
    demo.launch()
