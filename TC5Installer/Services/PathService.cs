using Avalonia.Platform.Storage;
using TC5Installer.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TC5Installer.Services;

/// <summary>
/// Implementation of path validation and analysis service
/// </summary>
public class PathService : IPathService
{
    private readonly IStorageProvider? _storageProvider;
    
    public PathService(IStorageProvider? storageProvider = null)
    {
        _storageProvider = storageProvider;
    }
    
    public bool IsValidDeploymentSource(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            return false;
            
        // Check for required subfolders
        foreach (var folder in DeploymentStructure.RequiredSubfolders)
        {
            if (!Directory.Exists(Path.Combine(sourcePath, folder)))
                return false;
        }
        
        // Check for required files
        foreach (var file in DeploymentStructure.RequiredFiles)
        {
            if (!File.Exists(Path.Combine(sourcePath, file)))
                return false;
        }
        
        return true;
    }
    
    public bool IsDirectoryEmpty(string path)
    {
        if (!Directory.Exists(path))
            return true;
            
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }
    
    public DeploymentInfo AnalyzeDeployment(string sourcePath, string targetPath)
    {
        var isSourceEmpty = IsDirectoryEmpty(sourcePath);
        var isTargetEmpty = IsDirectoryEmpty(targetPath);
        
        var hasLogs = Directory.Exists(Path.Combine(targetPath, "logs"));
        var hasData = Directory.Exists(Path.Combine(targetPath, "data"));
        var hasDeployment = IsValidDeploymentSource(targetPath);
        
        return new DeploymentInfo
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            IsSourceEmpty = isSourceEmpty,
            IsTargetEmpty = isTargetEmpty,
            HasLogsDirectory = hasLogs,
            HasDataDirectory = hasData,
            HasDeploymentDirectory = hasDeployment
        };
    }
    
    public async Task<string?> SelectFolderAsync(string title)
    {
        if (_storageProvider == null)
            return null;
            
        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };
        
        var result = await _storageProvider.OpenFolderPickerAsync(options);
        
        return result?.FirstOrDefault()?.Path.LocalPath;
    }
    
    public Task<bool> ValidateDeploymentStructureAsync(string sourcePath)
    {
        return Task.FromResult(IsValidDeploymentSource(sourcePath));
    }
    
    public Task<bool> IsDirectoryEmptyAsync(string path)
    {
        return Task.FromResult(IsDirectoryEmpty(path));
    }
    
    public Task<DeploymentInfo> AnalyzeDirectoryAsync(string sourcePath, string targetPath)
    {
        return Task.FromResult(AnalyzeDeployment(sourcePath, targetPath));
    }
}