namespace Chronocode.Data.Models;

/// <summary>
/// Represents version information following semantic versioning (SemVer) principles
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// The semantic version string (e.g., "1.0.0")
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The release date of this version
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Brief description of this version
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of new features in this version
    /// </summary>
    public List<string> NewFeatures { get; set; } = new();

    /// <summary>
    /// List of improvements in this version
    /// </summary>
    public List<string> Improvements { get; set; } = new();

    /// <summary>
    /// List of bug fixes in this version
    /// </summary>
    public List<string> BugFixes { get; set; } = new();

    /// <summary>
    /// List of breaking changes in this version
    /// </summary>
    public List<string> BreakingChanges { get; set; } = new();

    /// <summary>
    /// Whether this is a major, minor, or patch release
    /// </summary>
    public VersionType Type { get; set; }

    /// <summary>
    /// Indicates if this is the current version
    /// </summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Types of version releases following semantic versioning
/// </summary>
public enum VersionType
{
    /// <summary>
    /// Patch release (bug fixes) - increments Z in X.Y.Z
    /// </summary>
    Patch,
    
    /// <summary>
    /// Minor release (new features, backward compatible) - increments Y in X.Y.Z
    /// </summary>
    Minor,
    
    /// <summary>
    /// Major release (breaking changes) - increments X in X.Y.Z
    /// </summary>
    Major
}
