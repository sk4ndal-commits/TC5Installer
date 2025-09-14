using TC5Installer.Models;
using System;
using System.Threading.Tasks;

namespace TC5Installer.Services;

/// <summary>
/// Service for file and directory operations during installation
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Copies deployment from source to target with optional preservation of logs/data and optionally keeping existing deployment.
    /// </summary>
    Task<bool> CopyDeploymentAsync(DeploymentInfo deploymentInfo, bool preserveLogs, bool preserveData, bool keepDeployment,
        IProgress<string>? progress = null);

    /// <summary>
    /// Removes logs directory from target path
    /// </summary>
    Task<bool> RemoveLogsDirectoryAsync(string targetPath);

    /// <summary>
    /// Removes data directory from target path
    /// </summary>
    Task<bool> RemoveDataDirectoryAsync(string targetPath);

    /// <summary>
    /// Backs up existing logs directory before copy operation
    /// </summary>
    Task<bool> BackupLogsDirectoryAsync(string targetPath);

    /// <summary>
    /// Backs up existing data directory before copy operation
    /// </summary>
    Task<bool> BackupDataDirectoryAsync(string targetPath);
}