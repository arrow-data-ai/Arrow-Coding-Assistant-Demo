using System.Text.RegularExpressions;
using Chronocode.Data.Models;

namespace Chronocode.Services;

/// <summary>
/// Service for parsing CHANGELOG.md file to extract version information
/// </summary>
public class ChangelogService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ChangelogService> _logger;

    public ChangelogService(IWebHostEnvironment environment, ILogger<ChangelogService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Parse the CHANGELOG.md file and return version information
    /// </summary>
    public async Task<List<VersionInfo>> ParseChangelogAsync()
    {
        try
        {
            var changelogPath = Path.Combine(_environment.ContentRootPath, "Documentation", "CHANGELOG.md");
            
            if (!File.Exists(changelogPath))
            {
                _logger.LogWarning("CHANGELOG.md file not found at {Path}", changelogPath);
                return new List<VersionInfo>();
            }

            var content = await File.ReadAllTextAsync(changelogPath);
            return ParseChangelogContent(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CHANGELOG.md");
            return new List<VersionInfo>();
        }
    }

    /// <summary>
    /// Parse the changelog content and extract version information
    /// </summary>
    private List<VersionInfo> ParseChangelogContent(string content)
    {
        var versions = new List<VersionInfo>();
        
        // Pattern to match version headers: ## [1.0.0] - 2025-07-30
        var versionPattern = @"## \[(\d+\.\d+\.\d+)\] - (\d{4}-\d{2}-\d{2})";
        
        var versionMatches = Regex.Matches(content, versionPattern);

        foreach (Match versionMatch in versionMatches)
        {
            var version = versionMatch.Groups[1].Value;
            var dateStr = versionMatch.Groups[2].Value;

            if (DateTime.TryParse(dateStr, out var releaseDate))
            {
                var versionInfo = new VersionInfo
                {
                    Version = version,
                    ReleaseDate = releaseDate,
                    Description = $"Release {version}",
                    Type = DetermineVersionType(version),
                    NewFeatures = new List<string>(),
                    Improvements = new List<string>(),
                    BugFixes = new List<string>(),
                    BreakingChanges = new List<string>()
                };

                // Extract the content for this version
                var versionContent = ExtractVersionContent(content, versionMatch);
                
                // Parse sections within this version
                ParseVersionSections(versionContent, versionInfo);

                versions.Add(versionInfo);
            }
        }

        return versions.OrderByDescending(v => ParseVersion(v.Version)).ToList();
    }

    /// <summary>
    /// Extract content between version headers
    /// </summary>
    private static string ExtractVersionContent(string content, Match versionMatch)
    {
        var startIndex = versionMatch.Index;
        var nextVersionIndex = content.IndexOf("\n## [", startIndex + 1);
        
        if (nextVersionIndex > 0)
        {
            return content.Substring(startIndex, nextVersionIndex - startIndex);
        }
        else
        {
            return content.Substring(startIndex);
        }
    }

    /// <summary>
    /// Parse sections (Added, Changed, Fixed, etc.) within a version
    /// </summary>
    private static void ParseVersionSections(string versionContent, VersionInfo versionInfo)
    {
        // Pattern to match sections: ### Added\n- Item 1\n- Item 2
        var sectionPattern = @"### (Added|Changed|Fixed|Security|Deprecated|Removed)\s*\n((?:- .+\n?)*)";
        var sectionMatches = Regex.Matches(versionContent, sectionPattern, RegexOptions.Multiline);

        foreach (Match sectionMatch in sectionMatches)
        {
            var sectionType = sectionMatch.Groups[1].Value;
            var itemsText = sectionMatch.Groups[2].Value;
            
            var items = itemsText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Trim().StartsWith("- "))
                .Select(line => line.Trim().Substring(2).Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            switch (sectionType.ToLower())
            {
                case "added":
                    versionInfo.NewFeatures.AddRange(items);
                    break;
                case "changed":
                    versionInfo.Improvements.AddRange(items);
                    break;
                case "fixed":
                    versionInfo.BugFixes.AddRange(items);
                    break;
                case "deprecated":
                case "removed":
                    versionInfo.BreakingChanges.AddRange(items);
                    break;
                case "security":
                    // Add security items as improvements
                    versionInfo.Improvements.AddRange(items.Select(item => $"🔒 {item}"));
                    break;
            }
        }

        // Extract description from the first paragraph after the version header
        // Only capture standalone paragraphs, not section content starting with ###
        var descriptionMatch = Regex.Match(versionContent, @"## \[[\d\.]+\] - [\d-]+\s*\n\s*([^#\n].*?)(?=\n###|\n\n|\Z)", RegexOptions.Singleline);
        if (descriptionMatch.Success && !string.IsNullOrWhiteSpace(descriptionMatch.Groups[1].Value))
        {
            var description = descriptionMatch.Groups[1].Value.Trim();
            // Only use the description if it doesn't start with section markers
            if (!description.StartsWith("###"))
            {
                versionInfo.Description = description;
            }
        }
        
        // If no standalone description found, generate one based on content
        if (string.IsNullOrEmpty(versionInfo.Description) || versionInfo.Description == $"Release {versionInfo.Version}")
        {
            // Generate description based on content
            var featureCount = versionInfo.NewFeatures.Count;
            var improvementCount = versionInfo.Improvements.Count;
            var bugFixCount = versionInfo.BugFixes.Count;

            if (featureCount > 0 || improvementCount > 0 || bugFixCount > 0)
            {
                var parts = new List<string>();
                if (featureCount > 0) parts.Add($"{featureCount} new feature{(featureCount > 1 ? "s" : "")}");
                if (improvementCount > 0) parts.Add($"{improvementCount} improvement{(improvementCount > 1 ? "s" : "")}");
                if (bugFixCount > 0) parts.Add($"{bugFixCount} bug fix{(bugFixCount > 1 ? "es" : "")}");

                versionInfo.Description = $"Release with {string.Join(", ", parts)}";
            }
        }
    }

    /// <summary>
    /// Determine version type based on semantic versioning rules
    /// </summary>
    private static VersionType DetermineVersionType(string version)
    {
        var parts = version.Split('.');
        if (parts.Length >= 3)
        {
            // Major version: X.0.0 where X > 0
            if (parts[1] == "0" && parts[2] == "0" && parts[0] != "0")
                return VersionType.Major;
            
            // Minor version: X.Y.0 where Y > 0
            if (parts[2] == "0" && parts[1] != "0")
                return VersionType.Minor;
        }
        
        // Patch version: X.Y.Z where Z > 0
        return VersionType.Patch;
    }

    /// <summary>
    /// Parse version string for comparison
    /// </summary>
    private static Version ParseVersion(string versionString)
    {
        try
        {
            return new Version(versionString);
        }
        catch
        {
            return new Version("0.0.0");
        }
    }
}
