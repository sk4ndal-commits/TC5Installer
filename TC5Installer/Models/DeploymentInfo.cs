namespace TC5Installer.Models;

/// <summary>
/// Represents information about a deployment folder structure
/// </summary>
public record DeploymentInfo
{
    public string SourcePath { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
    public bool IsSourceEmpty { get; init; }
    public bool IsTargetEmpty { get; init; }
    public bool HasLogsDirectory { get; init; }
    public bool HasDataDirectory { get; init; }
    public bool HasDeploymentDirectory { get; init; }
}

/// <summary>
/// Represents the expected deployment folder structure
/// </summary>
public static class DeploymentStructure
{
    public static readonly string[] RequiredSubfolders = { "bin", "conf", "docs", "FabLink" };
    public static readonly string[] RequiredFiles = { "launcher_X64.bat", "launcher_X64_Simulation.bat" };
    public static readonly string[] PreservableDirectories = { "logs", "data" };
}