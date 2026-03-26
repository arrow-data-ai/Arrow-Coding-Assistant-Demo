using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for managing work authorization artifacts
/// </summary>
public class WorkAuthorizationArtifactService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WorkAuthorizationArtifactService> _logger;

    // Allowed file types for security
    private readonly HashSet<string> _allowedContentTypes = new()
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "image/jpeg",
        "image/png",
        "image/gif",
        "application/vnd.ms-outlook", // .msg files
        "message/rfc822", // email files
        "application/octet-stream" // Generic binary - we'll validate by extension
    };

    // Allowed file extensions (fallback when MIME type is missing or generic)
    private readonly HashSet<string> _allowedExtensions = new()
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".msg"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB limit

    public WorkAuthorizationArtifactService(
        ApplicationDbContext context, 
        IWebHostEnvironment environment,
        ILogger<WorkAuthorizationArtifactService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Gets all artifacts for a specific charge code
    /// </summary>
    public async Task<List<WorkAuthorizationArtifact>> GetArtifactsByChargeCodeAsync(int chargeCodeId)
    {
        return await _context.WorkAuthorizationArtifacts
            .Include(waa => waa.UploadedByUser)
            .Where(waa => waa.ChargeCodeId == chargeCodeId && waa.IsActive)
            .OrderByDescending(waa => waa.UploadedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific artifact by ID
    /// </summary>
    public async Task<WorkAuthorizationArtifact?> GetArtifactByIdAsync(int artifactId)
    {
        return await _context.WorkAuthorizationArtifacts
            .Include(waa => waa.ChargeCode)
            .Include(waa => waa.UploadedByUser)
            .FirstOrDefaultAsync(waa => waa.Id == artifactId && waa.IsActive);
    }

    /// <summary>
    /// Uploads a new artifact for a charge code
    /// </summary>
    public async Task<(bool Success, string Message, WorkAuthorizationArtifact? Artifact)> UploadArtifactAsync(
        int chargeCodeId, 
        IFormFile file, 
        string description, 
        string userId)
    {
        try
        {
            _logger.LogInformation("Attempting to upload artifact: FileName={FileName}, ContentType={ContentType}, Size={Size}, ChargeCodeId={ChargeCodeId}", 
                file.FileName, file.ContentType, file.Length, chargeCodeId);

            // Validate file
            var validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                _logger.LogWarning("File validation failed: {ErrorMessage}", validation.ErrorMessage);
                return (false, validation.ErrorMessage, null);
            }

            // Ensure charge code exists
            var chargeCode = await _context.ChargeCodes.FindAsync(chargeCodeId);
            if (chargeCode == null)
            {
                return (false, "Charge code not found.", null);
            }

            // Create upload directory if it doesn't exist
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "work-authorization");
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Normalize content type (browsers often don't send correct MIME type for .msg files)
            var contentType = NormalizeContentType(file.ContentType, fileExtension);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Create artifact record
            var artifact = new WorkAuthorizationArtifact
            {
                ChargeCodeId = chargeCodeId,
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                ContentType = contentType,
                FileSizeBytes = file.Length,
                FilePath = Path.Combine("uploads", "work-authorization", uniqueFileName),
                Description = description,
                UploadedByUserId = userId,
                UploadedDate = DateTime.UtcNow
            };

            _context.WorkAuthorizationArtifacts.Add(artifact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Work authorization artifact uploaded successfully: {FileName} for charge code {ChargeCodeId}", 
                file.FileName, chargeCodeId);

            return (true, "File uploaded successfully.", artifact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading work authorization artifact for charge code {ChargeCodeId}", chargeCodeId);
            return (false, "An error occurred while uploading the file.", null);
        }
    }

    /// <summary>
    /// Deletes an artifact (hard delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteArtifactAsync(int artifactId, string userId)
    {
        try
        {
            var artifact = await _context.WorkAuthorizationArtifacts.FindAsync(artifactId);
            if (artifact == null)
            {
                return (false, "Artifact not found.");
            }

            // Delete file from disk
            var fullPath = Path.Combine(_environment.WebRootPath, artifact.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Remove artifact from database
            _context.WorkAuthorizationArtifacts.Remove(artifact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Work authorization artifact hard deleted: {ArtifactId} by user {UserId}", artifactId, userId);

            return (true, "Artifact deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting work authorization artifact {ArtifactId}", artifactId);
            return (false, "An error occurred while deleting the artifact.");
        }
    }

    /// <summary>
    /// Gets the file stream for downloading an artifact
    /// </summary>
    public async Task<(Stream? FileStream, string? ContentType, string? FileName)> GetArtifactFileAsync(int artifactId)
    {
        var artifact = await GetArtifactByIdAsync(artifactId);
        if (artifact == null)
        {
            return (null, null, null);
        }

        var fullPath = Path.Combine(_environment.WebRootPath, artifact.FilePath.Replace('/', Path.DirectorySeparatorChar));
        
        if (!File.Exists(fullPath))
        {
            return (null, null, null);
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (fileStream, artifact.ContentType, artifact.OriginalFileName);
    }

    /// <summary>
    /// Validates the uploaded file
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Please select a file to upload.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, $"File size exceeds the maximum limit of {MaxFileSizeBytes / (1024 * 1024)}MB.");
        }

        // Additional filename validation
        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
        {
            return (false, "Invalid file name.");
        }

        // Get file extension
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

        // Validate by content type if provided and not generic
        bool validContentType = !string.IsNullOrWhiteSpace(contentType) && 
                                contentType != "application/octet-stream" &&
                                _allowedContentTypes.Contains(contentType);

        // Validate by file extension (fallback or additional check)
        bool validExtension = _allowedExtensions.Contains(fileExtension);

        // File must pass either content type check OR extension check
        if (!validContentType && !validExtension)
        {
            return (false, $"File type not allowed. Received content type: '{file.ContentType}', extension: '{fileExtension}'. Please upload PDF, Word, Excel, text, image, or .msg email files only.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Generates a URL for viewing a PDF artifact inline
    /// </summary>
    public string? GetArtifactUrl(int artifactId)
    {
        var artifact = _context.WorkAuthorizationArtifacts.FirstOrDefault(a => a.Id == artifactId);
        if (artifact == null || artifact.ContentType != "application/pdf")
        {
            return null;
        }

        return $"/uploads/work-authorization/{artifact.FileName}";
    }

    /// <summary>
    /// Checks if an artifact can be viewed inline based on its content type
    /// </summary>
    public bool CanViewInline(string contentType)
    {
        var viewableTypes = new HashSet<string>
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/gif",
            "text/plain",
            "application/vnd.ms-outlook", // .msg files
            "message/rfc822" // email files
        };

        return viewableTypes.Contains(contentType.ToLowerInvariant());
    }

    /// <summary>
    /// Gets the inline viewer URL for an artifact
    /// </summary>
    public string GetInlineViewUrl(int artifactId)
    {
        return $"/api/artifact/view/{artifactId}";
    }

    /// <summary>
    /// Normalizes content type based on file extension when browser doesn't provide correct MIME type
    /// </summary>
    private string NormalizeContentType(string? browserContentType, string fileExtension)
    {
        // If browser provided a valid, specific content type, use it
        if (!string.IsNullOrWhiteSpace(browserContentType) && 
            browserContentType != "application/octet-stream" &&
            _allowedContentTypes.Contains(browserContentType.ToLowerInvariant()))
        {
            return browserContentType;
        }

        // Otherwise, determine content type from file extension
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".msg" => "application/vnd.ms-outlook",
            _ => browserContentType ?? "application/octet-stream"
        };
    }
}
