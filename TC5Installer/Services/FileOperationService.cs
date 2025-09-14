using TC5Installer.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TC5Installer.Services;

/// <summary>
/// Implementation of file and directory operations during installation
/// </summary>
public class FileOperationService : IFileOperationService
{
    public async Task<bool> CopyDeploymentAsync(DeploymentInfo deploymentInfo, bool preserveLogs, bool preserveData, bool keepDeployment, IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report("⏳ Preparing installation...");
            await Task.Delay(200);
            
            // First backup logs/data if they exist and should be preserved
            if (deploymentInfo.HasLogsDirectory && preserveLogs)
            {
                progress?.Report("Backing up logs directory...");
                await BackupLogsDirectoryAsync(deploymentInfo.TargetPath);
            }
            
            if (deploymentInfo.HasDataDirectory && preserveData)
            {
                progress?.Report("Backing up data directory...");
                await BackupDataDirectoryAsync(deploymentInfo.TargetPath);
            }
            
            // Copy deployment structure (unless user opts to keep existing deployment)
            if (keepDeployment && deploymentInfo.HasDeploymentDirectory)
            {
                progress?.Report("Keeping existing deployment as requested. Skipping overwrite of deployment files...");
            }
            else
            {
                progress?.Report("📦 Installing files...");
                await CopyDirectoryRecursiveAsync(deploymentInfo.SourcePath, deploymentInfo.TargetPath, progress);
            }
            
            // Post install cleanup and preservation
            progress?.Report("🧹 Post-install cleanup...");
            if (deploymentInfo.HasLogsDirectory && !preserveLogs)
            {
                progress?.Report("Removing logs directory...");
                await RemoveLogsDirectoryAsync(deploymentInfo.TargetPath);
            }
            
            if (deploymentInfo.HasDataDirectory && !preserveData)
            {
                progress?.Report("Removing data directory...");
                await RemoveDataDirectoryAsync(deploymentInfo.TargetPath);
            }
            
            // Restore backed up directories if preserved
            if (deploymentInfo.HasLogsDirectory && preserveLogs)
            {
                progress?.Report("Restoring logs directory...");
                await RestoreBackupAsync(deploymentInfo.TargetPath, "logs");
            }
            
            if (deploymentInfo.HasDataDirectory && preserveData)
            {
                progress?.Report("Restoring data directory...");
                await RestoreBackupAsync(deploymentInfo.TargetPath, "data");
            }
            
            await Task.Delay(150);
            progress?.Report("✅ Installation steps completed.");
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error during deployment copy: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> RemoveLogsDirectoryAsync(string targetPath)
    {
        return await RemoveDirectoryAsync(Path.Combine(targetPath, "logs"));
    }
    
    public async Task<bool> RemoveDataDirectoryAsync(string targetPath)
    {
        return await RemoveDirectoryAsync(Path.Combine(targetPath, "data"));
    }
    
    public async Task<bool> BackupLogsDirectoryAsync(string targetPath)
    {
        return await BackupDirectoryAsync(targetPath, "logs");
    }
    
    public async Task<bool> BackupDataDirectoryAsync(string targetPath)
    {
        return await BackupDirectoryAsync(targetPath, "data");
    }
    
    private async Task<bool> BackupDirectoryAsync(string targetPath, string directoryName)
    {
        try
        {
            var sourceDir = Path.Combine(targetPath, directoryName);
            var backupDir = Path.Combine(targetPath, $"{directoryName}_backup");
            
            if (Directory.Exists(sourceDir))
            {
                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }
                
                await Task.Run(() => CopyDirectory(sourceDir, backupDir));
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> RestoreBackupAsync(string targetPath, string directoryName)
    {
        try
        {
            var backupDir = Path.Combine(targetPath, $"{directoryName}_backup");
            var targetDir = Path.Combine(targetPath, directoryName);
            
            if (Directory.Exists(backupDir))
            {
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                
                await Task.Run(() => Directory.Move(backupDir, targetDir));
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> RemoveDirectoryAsync(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                await Task.Run(() => Directory.Delete(path, true));
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task CopyDirectoryRecursiveAsync(string sourceDir, string destinationDir, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
            
            Directory.CreateDirectory(destinationDir);
            
            foreach (FileInfo file in dir.GetFiles())
            {
                progress?.Report($"Copying file: {file.Name}");
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }
            
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                progress?.Report($"Copying directory: {subDir.Name}");
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        });
    }
    
    private void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
            return;
        
        Directory.CreateDirectory(destinationDir);
        
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }
        
        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}