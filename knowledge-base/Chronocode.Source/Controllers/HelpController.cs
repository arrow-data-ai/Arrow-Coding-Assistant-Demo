using Microsoft.AspNetCore.Mvc;
using Chronocode.Services;

namespace Chronocode.Controllers
{
    public class HelpController : Controller
    {
        private readonly HelpService _helpService;

        public HelpController(HelpService helpService)
        {
            _helpService = helpService;
        }

        [Route("/help/{topic}")]
        public async Task<IActionResult> ShowHelp(string topic)
        {
            // Debug logging
            Console.WriteLine($"HelpController.ShowHelp called with topic: {topic}");
            
            try
            {
                var (content, title) = await _helpService.GetHelpContentAsync(topic);
                
                Console.WriteLine($"Help content loaded for topic: {topic}, title: {title}");
                
                // Create a simple HTML page with the help content
                var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{title} - ChronoCode Help</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css"" rel=""stylesheet"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f8f9fa;
        }}
        .help-container {{
            max-width: 900px;
            margin: 0 auto;
            padding: 2rem;
        }}
        .help-content {{
            background: white;
            border-radius: 8px;
            padding: 2rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            line-height: 1.6;
        }}
        .help-content h1 {{
            color: #0366d6;
            border-bottom: 1px solid #e1e4e8;
            padding-bottom: 0.3em;
            margin-bottom: 1em;
        }}
        .help-content h2 {{
            color: #24292e;
            border-bottom: 1px solid #e1e4e8;
            padding-bottom: 0.3em;
            margin-top: 2em;
            margin-bottom: 1em;
        }}
        .help-content h3 {{
            color: #24292e;
            margin-top: 1.5em;
            margin-bottom: 0.5em;
        }}
        .help-content code {{
            background-color: rgba(27,31,35,0.05);
            border-radius: 3px;
            font-size: 85%;
            margin: 0;
            padding: 0.2em 0.4em;
        }}
        .help-content pre {{
            background-color: #f6f8fa;
            border-radius: 6px;
            font-size: 85%;
            line-height: 1.45;
            overflow: auto;
            padding: 16px;
        }}
        .help-content blockquote {{
            border-left: 0.25em solid #dfe2e5;
            color: #6a737d;
            margin: 0;
            padding: 0 1em;
        }}
        .help-content table {{
            border-collapse: collapse;
            margin: 1em 0;
            width: 100%;
        }}
        .help-content th,
        .help-content td {{
            border: 1px solid #dfe2e5;
            padding: 6px 13px;
        }}
        .help-content th {{
            background-color: #f6f8fa;
            font-weight: 600;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 1rem 0;
            margin-bottom: 2rem;
        }}
        .back-button {{
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 1000;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""container"">
            <h1 class=""m-0""><i class=""fas fa-question-circle me-2""></i>ChronoCode Help</h1>
        </div>
    </div>
    
    <button class=""btn btn-primary back-button"" onclick=""window.close()"">
        <i class=""fas fa-times me-2""></i>Close
    </button>
    
    <div class=""help-container"">
        <div class=""help-content"">
            <h1>{title}</h1>
            {content}
        </div>
    </div>
</body>
</html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HelpController.ShowHelp: {ex.Message}");
                return BadRequest($"Error loading help content: {ex.Message}");
            }
        }
    }
}
