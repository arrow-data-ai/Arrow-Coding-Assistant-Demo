using System.Reflection;
using Chronocode.Data.Models;

namespace Chronocode.Services;

/// <summary>
/// Service for managing application version information and changelog
/// </summary>
public class VersionService
{
    private readonly ChangelogService _changelogService;
    private readonly ILogger<VersionService> _logger;
    private List<VersionInfo>? _cachedVersionHistory;

    public VersionService(ChangelogService changelogService, ILogger<VersionService> logger)
    {
        _changelogService = changelogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current application version from assembly
    /// </summary>
    public static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0"; // Returns X.Y.Z format
    }

    /// <summary>
    /// Gets the current version information with changelog
    /// </summary>
    public async Task<VersionInfo> GetCurrentVersionInfoAsync()
    {
        var currentVersion = GetCurrentVersion();
        var versionHistory = await GetVersionHistoryAsync();
        var versionInfo = versionHistory.Find(v => v.Version == currentVersion);
        
        if (versionInfo != null)
        {
            versionInfo.IsCurrent = true;
            return versionInfo;
        }

        // Fallback if version not found in changelog
        _logger.LogWarning("Current version {Version} not found in changelog, creating fallback", currentVersion);
        return new VersionInfo
        {
            Version = currentVersion,
            ReleaseDate = DateTime.Now,
            Description = "Current version",
            IsCurrent = true,
            Type = VersionType.Major
        };
    }

    /// <summary>
    /// Gets the current version information synchronously (for backwards compatibility)
    /// </summary>
    public VersionInfo GetCurrentVersionInfo()
    {
        try
        {
            // Use Task.Run to avoid deadlocks in Blazor
            return Task.Run(async () => await GetCurrentVersionInfoAsync()).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current version info, using fallback");
            return new VersionInfo
            {
                Version = GetCurrentVersion(),
                ReleaseDate = DateTime.Now,
                Description = "Current version",
                IsCurrent = true,
                Type = VersionType.Major
            };
        }
    }

    /// <summary>
    /// Gets all version history from changelog, ordered by version descending (newest first)
    /// </summary>
    public async Task<List<VersionInfo>> GetVersionHistoryAsync()
    {
        if (_cachedVersionHistory == null)
        {
            try
            {
                _cachedVersionHistory = await _changelogService.ParseChangelogAsync();
                
                // Mark the current version
                var currentVersion = GetCurrentVersion();
                var current = _cachedVersionHistory.Find(v => v.Version == currentVersion);
                if (current != null)
                {
                    current.IsCurrent = true;
                }
                
                _logger.LogInformation("Loaded {Count} versions from changelog", _cachedVersionHistory.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load version history from changelog, using fallback");
                _cachedVersionHistory = GetFallbackVersionHistory();
            }
        }

        return _cachedVersionHistory;
    }

    /// <summary>
    /// Gets all version history synchronously (for backwards compatibility)
    /// </summary>
    public List<VersionInfo> GetVersionHistory()
    {
        try
        {
            // Use Task.Run to avoid deadlocks in Blazor
            return Task.Run(async () => await GetVersionHistoryAsync()).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history, using fallback");
            return GetFallbackVersionHistory();
        }
    }

    /// <summary>
    /// Gets recent versions (last N releases)
    /// </summary>
    public async Task<List<VersionInfo>> GetRecentVersionsAsync(int count = 5)
    {
        var history = await GetVersionHistoryAsync();
        return history.Take(count).ToList();
    }

    /// <summary>
    /// Gets recent versions synchronously (for backwards compatibility)
    /// </summary>
    public List<VersionInfo> GetRecentVersions(int count = 5)
    {
        try
        {
            // Use Task.Run to avoid deadlocks in Blazor
            return Task.Run(async () => await GetRecentVersionsAsync(count)).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent versions, using fallback");
            return GetFallbackVersionHistory().Take(count).ToList();
        }
    }

    /// <summary>
    /// Refresh the cached version history (call when changelog is updated)
    /// </summary>
    public void RefreshCache()
    {
        _cachedVersionHistory = null;
    }

    /// <summary>
    /// Fallback version history if changelog parsing fails
    /// </summary>
    private static List<VersionInfo> GetFallbackVersionHistory()
    {
        return new List<VersionInfo>
        {
            new VersionInfo
            {
                Version = "1.0.0",
                ReleaseDate = new DateTime(2025, 7, 30, 0, 0, 0, DateTimeKind.Utc),
                Description = "Initial release of Chronocode with semantic versioning",
                Type = VersionType.Major,
                NewFeatures = new List<string>
                {
                    "Time tracking with start/stop functionality",
                    "Project and charge code management",
                    "Activity tracking and categorization",
                    "Comprehensive reporting system",
                    "User authentication and authorization",
                    "Role-based access control (RBAC)",
                    "Admin user management",
                    "SQLite database with Entity Framework Core",
                    "Responsive Blazor Server UI",
                    "Help system with comprehensive documentation",
                    "Version information and changelog display"
                },
                Improvements = new List<string>
                {
                    "Clean, modern Bootstrap-based UI",
                    "Secure local authentication",
                    "Containerized deployment support",
                    "Comprehensive logging and error handling"
                },
                BugFixes = new List<string>(),
                BreakingChanges = new List<string>(),
                IsCurrent = true
            }
        };
    }
}
