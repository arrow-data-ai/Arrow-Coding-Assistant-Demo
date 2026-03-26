using Markdig;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Chronocode.Services
{
    public class HelpService
    {
        private readonly string _documentationPath;
        private readonly MarkdownPipeline _markdownPipeline;
        
        private const string GettingStarted = "getting-started";
        private const string UserRoles = "user-roles";
        private const string Projects = "projects";
        private const string Tasks = "tasks";
        private const string Activities = "activities";
        private const string ChargeCodes = "charge-codes";
        private const string Reports = "reports";
        private const string UserManagement = "user-management";
        private const string SystemAdministration = "system-administration";
        private const string FAQ = "faq";
        private const string BestPractices = "best-practices";
        private const string Troubleshooting = "troubleshooting";
        private const string WorkAuthorizationArtifacts = "work-authorization-artifacts";
        private const string CoreFeatures = "Core Features";
        private const string Support = "Support";
        private const string GettingStartedCategory = "Getting Started";
        private const string Administration = "Administration";
        private const string General = "General";

        public HelpService(IWebHostEnvironment environment)
        {
            _documentationPath = Path.Combine(environment.ContentRootPath, "Documentation");
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public async Task<(string content, string title)> GetHelpContentAsync(string topic)
        {
            var fileName = GetFileNameForTopic(topic);
            var title = GetTitleForTopic(topic);
            
            try
            {
                var filePath = Path.Combine(_documentationPath, fileName);
                
                if (!File.Exists(filePath))
                {
                    return (GenerateFallbackContent(title), title);
                }

                var markdownContent = await File.ReadAllTextAsync(filePath);
                var htmlContent = Markdown.ToHtml(markdownContent, _markdownPipeline);
                
                // Wrap in a styled container
                var styledContent = $@"
                    <div class='help-content'>
                        {htmlContent}
                    </div>";
                
                return (styledContent, title);
            }
            catch (Exception ex)
            {
                return (GenerateErrorContent(ex.Message), "Error Loading Help");
            }
        }

        public Task<List<HelpTopic>> GetAllHelpTopicsAsync()
        {
            var topics = new List<HelpTopic>();
            
            try
            {
                if (!Directory.Exists(_documentationPath))
                {
                    return Task.FromResult(GetDefaultTopics());
                }

                var files = Directory.GetFiles(_documentationPath, "*.md");
                
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var topic = GetTopicForFileName(fileName);
                    var title = GetTitleForTopic(topic);
                    var category = GetCategoryForTopic(topic);
                    
                    topics.Add(new HelpTopic
                    {
                        Topic = topic,
                        Title = title,
                        Category = category,
                        FileName = fileName
                    });
                }
                
                return Task.FromResult(topics.OrderBy(t => t.Category).ThenBy(t => t.Title).ToList());
            }
            catch
            {
                return Task.FromResult(GetDefaultTopics());
            }
        }

        private static string GetFileNameForTopic(string topic)
        {
            return topic switch
            {
                GettingStarted => "Getting-Started.md",
                UserRoles => "User-Roles-and-Permissions.md",
                Projects => "Project-Management.md",
                Tasks => "Task-Management.md",
                Activities => "Activity-Tracking.md",
                ChargeCodes => "Charge-Code-Management.md",
                Reports => "Reporting.md",
                UserManagement => "User-Management.md",
                SystemAdministration => "System-Administration.md",
                FAQ => "FAQ.md",
                BestPractices => "Best-Practices.md",
                Troubleshooting => "Troubleshooting.md",
                WorkAuthorizationArtifacts => "Work-Authorization-Artifacts.md",
                _ => "README.md"
            };
        }

        private static string GetTitleForTopic(string topic)
        {
            return topic switch
            {
                GettingStarted => "Getting Started Guide",
                UserRoles => "User Roles & Permissions",
                Projects => "Project Management",
                Tasks => "Task Management",
                Activities => "Activity Tracking",
                ChargeCodes => "Charge Code Management",
                Reports => "Reporting",
                UserManagement => "User Management",
                SystemAdministration => "System Administration",
                FAQ => "Frequently Asked Questions",
                BestPractices => "Best Practices",
                Troubleshooting => "Troubleshooting",
                WorkAuthorizationArtifacts => "Work Authorization Artifacts",
                _ => "Chronocode Help"
            };
        }

        private static string GetTopicForFileName(string fileName)
        {
            return fileName switch
            {
                "Getting-Started.md" => GettingStarted,
                "User-Roles-and-Permissions.md" => UserRoles,
                "Project-Management.md" => Projects,
                "Task-Management.md" => Tasks,
                "Activity-Tracking.md" => Activities,
                "Charge-Code-Management.md" => ChargeCodes,
                "Reporting.md" => Reports,
                "User-Management.md" => UserManagement,
                "System-Administration.md" => SystemAdministration,
                "FAQ.md" => FAQ,
                "Best-Practices.md" => BestPractices,
                "Troubleshooting.md" => Troubleshooting,
                "Work-Authorization-Artifacts.md" => WorkAuthorizationArtifacts,
                "README.md" => "readme",
                _ => "unknown"
            };
        }

        private static string GetCategoryForTopic(string topic)
        {
            return topic switch
            {
                GettingStarted or UserRoles => GettingStartedCategory,
                Projects or Tasks or Activities or ChargeCodes or Reports or WorkAuthorizationArtifacts => CoreFeatures,
                UserManagement or SystemAdministration => Administration,
                FAQ or BestPractices or Troubleshooting => Support,
                _ => General
            };
        }

        private static List<HelpTopic> GetDefaultTopics()
        {
            return new List<HelpTopic>
            {
                new() { Topic = GettingStarted, Title = "Getting Started Guide", Category = GettingStartedCategory },
                new() { Topic = UserRoles, Title = "User Roles & Permissions", Category = GettingStartedCategory },
                new() { Topic = Projects, Title = "Project Management", Category = CoreFeatures },
                new() { Topic = Tasks, Title = "Task Management", Category = CoreFeatures },
                new() { Topic = Activities, Title = "Activity Tracking", Category = CoreFeatures },
                new() { Topic = ChargeCodes, Title = "Charge Code Management", Category = CoreFeatures },
                new() { Topic = WorkAuthorizationArtifacts, Title = "Work Authorization Artifacts", Category = CoreFeatures },
                new() { Topic = Reports, Title = "Reporting", Category = CoreFeatures },
                new() { Topic = UserManagement, Title = "User Management", Category = Administration },
                new() { Topic = FAQ, Title = "FAQ", Category = Support },
                new() { Topic = BestPractices, Title = "Best Practices", Category = Support },
                new() { Topic = Troubleshooting, Title = "Troubleshooting", Category = Support }
            };
        }

        private static string GenerateFallbackContent(string title)
        {
            return $@"
                <div class='alert alert-info'>
                    <h4><i class='fas fa-info-circle'></i> {title}</h4>
                    <p>Welcome to the help system for <strong>{title}</strong>.</p>
                    <p>This feature is designed to provide comprehensive guidance for using Chronocode effectively.</p>
                    <hr>
                    <p><strong>For immediate assistance:</strong></p>
                    <ul>
                        <li>Contact your system administrator</li>
                        <li>Check the FAQ section for common questions</li>
                        <li>Review the Getting Started guide</li>
                    </ul>
                </div>";
        }

        private static string GenerateErrorContent(string errorMessage)
        {
            return $@"
                <div class='alert alert-danger'>
                    <h4><i class='fas fa-exclamation-triangle'></i> Error Loading Help</h4>
                    <p>Could not load help content: {errorMessage}</p>
                    <hr>
                    <p><strong>Try:</strong></p>
                    <ul>
                        <li>Refresh the page</li>
                        <li>Contact your system administrator</li>
                    </ul>
                </div>";
        }
    }

    public class HelpTopic
    {
        public string Topic { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
