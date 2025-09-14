using TC5Installer.Models;
using System.Threading.Tasks;

namespace TC5Installer.Services;

/// <summary>
/// Service for path validation and analysis
/// </summary>
public interface IPathService
{
    /// <summary>
    /// Validates if the source path contains the required deployment structure
    /// </summary>
    bool IsValidDeploymentSource(string sourcePath);
    
    /// <summary>
    /// Validates if the source path contains the required deployment structure (async)
    /// </summary>
    Task<bool> ValidateDeploymentStructureAsync(string sourcePath);
    
    /// <summary>
    /// Checks if directory is empty
    /// </summary>
    bool IsDirectoryEmpty(string path);
    
    /// <summary>
    /// Checks if directory is empty (async)
    /// </summary>
    Task<bool> IsDirectoryEmptyAsync(string path);
    
    /// <summary>
    /// Analyzes deployment paths and returns information
    /// </summary>
    DeploymentInfo AnalyzeDeployment(string sourcePath, string targetPath);
    
    /// <summary>
    /// Analyzes directory for logs/data presence (async)
    /// </summary>
    Task<DeploymentInfo> AnalyzeDirectoryAsync(string sourcePath, string targetPath);
    
    /// <summary>
    /// Opens file dialog to select folder
    /// </summary>
    Task<string?> SelectFolderAsync(string title);
}