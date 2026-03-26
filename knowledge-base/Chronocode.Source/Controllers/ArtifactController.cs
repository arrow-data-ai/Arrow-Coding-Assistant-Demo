using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Chronocode.Services;
using Chronocode.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Chronocode.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ArtifactController : ControllerBase
{
    private readonly WorkAuthorizationArtifactService _artifactService;
    private readonly EmailViewerService _emailViewerService;
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<ArtifactController> _logger;

    public ArtifactController(
        WorkAuthorizationArtifactService artifactService,
        EmailViewerService emailViewerService,
        AuthorizationService authorizationService,
        ILogger<ArtifactController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _artifactService = artifactService;
        _emailViewerService = emailViewerService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadArtifact(int id)
    {
        try
        {
            // Get the artifact details first to check permissions
            var artifact = await _artifactService.GetArtifactByIdAsync(id);
            if (artifact == null)
            {
                return NotFound("Artifact not found.");
            }

            // Verify user has access to the project associated with this charge code
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !await _authorizationService.CanAccessProjectAsync(userId, artifact.ChargeCode.ProjectId))
            {
                _logger.LogWarning("Unauthorized artifact download attempt: UserId={UserId}, ArtifactId={ArtifactId}", userId, id);
                return Forbid();
            }

            // Get the file stream
            var (fileStream, contentType, fileName) = await _artifactService.GetArtifactFileAsync(id);
            
            if (fileStream == null || string.IsNullOrEmpty(fileName))
            {
                return NotFound("File not found.");
            }

            // Encode filename for secure HTTP header
            var encodedFileName = System.Net.WebUtility.UrlEncode(fileName);
            Response.Headers.Append("Content-Disposition", 
                $"attachment; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");

            return File(fileStream, contentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading artifact {ArtifactId}", id);
            return BadRequest("Error downloading file.");
        }
    }

    [HttpGet("view/{id}")]
    public async Task<IActionResult> ViewArtifact(int id)
    {
        try
        {
            // Get the artifact details first to check permissions
            var artifact = await _artifactService.GetArtifactByIdAsync(id);
            if (artifact == null)
            {
                return NotFound("Artifact not found.");
            }

            // Verify user has access to the project associated with this charge code
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !await _authorizationService.CanAccessProjectAsync(userId, artifact.ChargeCode.ProjectId))
            {
                _logger.LogWarning("Unauthorized artifact view attempt: UserId={UserId}, ArtifactId={ArtifactId}", userId, id);
                return Forbid();
            }

            // Get the file stream
            var (fileStream, contentType, fileName) = await _artifactService.GetArtifactFileAsync(id);
            
            if (fileStream == null || string.IsNullOrEmpty(fileName))
            {
                return NotFound("File not found.");
            }

            // Only allow safe content types to be viewed inline
            var safeInlineTypes = new[] { 
                "application/pdf", "image/jpeg", "image/png", "image/gif", 
                "text/plain", "application/vnd.ms-outlook" 
            };

            // Encode filename for secure HTTP header
            var encodedFileName = System.Net.WebUtility.UrlEncode(fileName);
            
            if (safeInlineTypes.Contains(contentType?.ToLowerInvariant()))
            {
                Response.Headers.Append("Content-Disposition", 
                    $"inline; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");
            }
            else
            {
                // Force download for potentially unsafe content types
                Response.Headers.Append("Content-Disposition", 
                    $"attachment; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");
            }
            
            return File(fileStream, contentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing artifact {ArtifactId}", id);
            return BadRequest("Error viewing file.");
        }
    }

    [HttpGet("email/{id}")]
    public async Task<IActionResult> GetEmailData(int id)
    {
        try
        {
            // Get the artifact to check if it's an email
            var artifact = await _artifactService.GetArtifactByIdAsync(id);
            if (artifact == null)
            {
                return NotFound("Artifact not found.");
            }

            // Verify user has access to the project associated with this charge code
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !await _authorizationService.CanAccessProjectAsync(userId, artifact.ChargeCode.ProjectId))
            {
                _logger.LogWarning("Unauthorized email view attempt: UserId={UserId}, ArtifactId={ArtifactId}", userId, id);
                return Forbid();
            }

            // Parse the email
            var emailData = await _emailViewerService.ParseEmailAsync(id);
            if (emailData == null)
            {
                return BadRequest("Failed to parse email file.");
            }

            return Ok(emailData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing email artifact {ArtifactId}", id);
            return BadRequest("Error parsing email file.");
        }
    }
}
