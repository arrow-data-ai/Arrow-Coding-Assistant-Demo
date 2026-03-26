using Chronocode.Data;
using Chronocode.Data.Models;
using MsgReader.Outlook;
using Ganss.Xss;

namespace Chronocode.Services;

/// <summary>
/// Service for parsing and rendering email files (.msg)
/// </summary>
public class EmailViewerService
{
    private readonly WorkAuthorizationArtifactService _artifactService;
    private readonly ILogger<EmailViewerService> _logger;
    private readonly HtmlSanitizer _sanitizer;

    public EmailViewerService(
        WorkAuthorizationArtifactService artifactService,
        ILogger<EmailViewerService> logger)
    {
        _artifactService = artifactService;
        _logger = logger;
        
        // Configure HTML sanitizer for security
        _sanitizer = new HtmlSanitizer();
        
        // Allow inline style attribute but NOT style tags (prevents CSS injection)
        _sanitizer.AllowedAttributes.Add("style");
        _sanitizer.AllowedCssProperties.Clear();
        
        // Allow only safe CSS properties (removed padding, margin, border, width, height to prevent layout manipulation)
        var safeCssProperties = new[]
        {
            "color", "background-color", "font-size", "font-family", "font-weight", "text-align"
        };
        
        foreach (var prop in safeCssProperties)
        {
            _sanitizer.AllowedCssProperties.Add(prop);
        }
    }

    /// <summary>
    /// Parses an .msg file and returns structured email data
    /// </summary>
    public async Task<EmailData?> ParseEmailAsync(int artifactId)
    {
        try
        {
            var (fileStream, contentType, fileName) = await _artifactService.GetArtifactFileAsync(artifactId);
            
            if (fileStream == null)
            {
                _logger.LogWarning("Artifact {ArtifactId} file not found", artifactId);
                return null;
            }

            // Check if it's an .msg file
            if (contentType?.ToLowerInvariant() != "application/vnd.ms-outlook")
            {
                _logger.LogWarning("Artifact {ArtifactId} is not an .msg file. ContentType: {ContentType}", artifactId, contentType);
                fileStream.Dispose();
                return null;
            }

            using (fileStream)
            using (var msg = new Storage.Message(fileStream))
            {
                var emailData = new EmailData
                {
                    Subject = msg.Subject ?? "(No Subject)",
                    From = FormatEmailAddress(msg.Sender),
                    To = FormatRecipients(msg.Recipients),
                    Cc = string.Empty, // MsgReader groups all recipients together
                    Bcc = string.Empty,
                    SentDate = msg.SentOn?.DateTime ?? DateTime.MinValue,
                    BodyHtml = SanitizeHtml(msg.BodyHtml),
                    BodyText = msg.BodyText,
                    Attachments = ParseAttachments(msg.Attachments, artifactId),
                    Importance = msg.ImportanceText ?? "Normal",
                    HasAttachments = msg.Attachments?.Count > 0
                };

                _logger.LogInformation("Successfully parsed email artifact {ArtifactId}", artifactId);
                return emailData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing email artifact {ArtifactId}", artifactId);
            return null;
        }
    }

    private string FormatEmailAddress(Storage.Sender? sender)
    {
        if (sender == null) return string.Empty;
        
        var displayName = sender.DisplayName;
        var email = sender.Email;
        
        if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(email))
        {
            return $"{displayName} <{email}>";
        }
        
        return email ?? displayName ?? string.Empty;
    }

    private string FormatRecipients(List<Storage.Recipient>? recipients)
    {
        if (recipients == null || !recipients.Any())
            return string.Empty;

        return string.Join("; ", recipients.Select(r =>
        {
            if (!string.IsNullOrEmpty(r.DisplayName) && !string.IsNullOrEmpty(r.Email))
            {
                return $"{r.DisplayName} <{r.Email}>";
            }
            return r.Email ?? r.DisplayName ?? string.Empty;
        }));
    }

    private string? SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        try
        {
            return _sanitizer.Sanitize(html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sanitizing HTML content");
            return null;
        }
    }

    private List<EmailAttachment> ParseAttachments(List<object>? msgAttachments, int artifactId)
    {
        var attachments = new List<EmailAttachment>();
        
        if (msgAttachments == null || msgAttachments.Count == 0)
            return attachments;

        foreach (var attachmentObj in msgAttachments)
        {
            try
            {
                // MsgReader returns different attachment types - try to extract common properties
                if (attachmentObj is Storage.Attachment attachment)
                {
                    attachments.Add(new EmailAttachment
                    {
                        FileName = attachment.FileName ?? "Unknown",
                        Size = attachment.Data?.Length ?? 0,
                        ContentId = attachment.ContentId,
                        IsInline = !string.IsNullOrEmpty(attachment.ContentId)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing attachment in artifact {ArtifactId}", artifactId);
            }
        }

        return attachments;
    }
}

/// <summary>
/// Structured email data
/// </summary>
public class EmailData
{
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
    public DateTime SentDate { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new();
    public string Importance { get; set; } = string.Empty;
    public bool HasAttachments { get; set; }
}

/// <summary>
/// Email attachment metadata
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentId { get; set; }
    public bool IsInline { get; set; }
}
