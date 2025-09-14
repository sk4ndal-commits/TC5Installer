using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TC5Installer.Models;
using TC5Installer.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TC5Installer.ViewModels;

/// <summary>
/// ViewModel for the installer functionality
/// </summary>
public partial class InstallerViewModel : ObservableObject
{
    public InstallerViewModel(){}
    
    private readonly IPathService _pathService;
    private readonly IFileOperationService _fileOperationService;
    private readonly Progress<string> _progressReporter;

    public InstallerViewModel(IPathService pathService, IFileOperationService fileOperationService)
    {
        _pathService = pathService;
        _fileOperationService = fileOperationService;
        _progressReporter = new Progress<string>(message => ProgressMessage = message);
        
        SelectSourceCommand = new AsyncRelayCommand(SelectSourcePathAsync);
        SelectTargetCommand = new AsyncRelayCommand(SelectTargetPathAsync);
        InstallCommand = new AsyncRelayCommand(ExecuteInstallationAsync, CanExecuteInstallation);
        
        // Initialize defaults
        InstallButtonText = "Install";
        HasBlockingWarning = false;
    }

    [ObservableProperty]
    private string? _sourcePath;

    [ObservableProperty]
    private string? _targetPath;

    [ObservableProperty]
    private string? _progressMessage;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private bool _showLogsOption;

    [ObservableProperty]
    private bool _showDataOption;

    [ObservableProperty]
    private bool _showDeploymentOption;

    [ObservableProperty]
    private bool _keepLogs = true; // Default to keep logs (opt-out)

    [ObservableProperty]
    private bool _keepData = true; // Default to keep data (opt-out)

    [ObservableProperty]
    private bool _keepDeployment; // Default to replace deployment (opt-in to keep)

    [ObservableProperty]
    private string? _warningMessage;

    [ObservableProperty]
    private DeploymentInfo? _deploymentInfo;

    // Indicates if current warning should block installation
    [ObservableProperty]
    private bool _hasBlockingWarning;

    // Text displayed on the install button (Install -> Installing… -> Done)
    [ObservableProperty]
    private string _installButtonText = "Install";

    public ICommand SelectSourceCommand { get; }
    public ICommand SelectTargetCommand { get; }
    public ICommand InstallCommand { get; }

    /// <summary>
    /// Opens dialog to select source deployment path
    /// </summary>
    private async Task SelectSourcePathAsync()
    {
        try
        {
            var selectedPath = await _pathService.SelectFolderAsync("Select Source Deployment Folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                SourcePath = selectedPath;
                await ValidateSourcePathAsync();
                UpdateInstallButtonState();
            }
        }
        catch (Exception ex)
        {
            WarningMessage = $"Error selecting source path: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens dialog to select target installation path
    /// </summary>
    private async Task SelectTargetPathAsync()
    {
        try
        {
            var selectedPath = await _pathService.SelectFolderAsync("Select Target Installation Folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                TargetPath = selectedPath;
                await ValidateTargetPathAsync();
                UpdateInstallButtonState();
            }
        }
        catch (Exception ex)
        {
            WarningMessage = $"Error selecting target path: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates the selected source path
    /// </summary>
    private async Task ValidateSourcePathAsync()
    {
        if (string.IsNullOrEmpty(SourcePath))
            return;

        try
        {
            var isEmpty = await _pathService.IsDirectoryEmptyAsync(SourcePath);
            if (isEmpty)
            {
                WarningMessage = "⚠️ Source directory is empty!";
                return;
            }

            var hasDeploymentStructure = await _pathService.ValidateDeploymentStructureAsync(SourcePath);
            if (!hasDeploymentStructure)
            {
                WarningMessage = "⚠️ Source directory doesn't contain expected deployment structure (bin, conf, docs, FabLink folders and launcher files).";
                return;
            }

            WarningMessage = null;
            ProgressMessage = "✅ Valid deployment source selected";
        }
        catch (Exception ex)
        {
            WarningMessage = $"Error validating source path: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates the selected target path and analyzes existing content
    /// </summary>
    private async Task ValidateTargetPathAsync()
    {
        if (string.IsNullOrEmpty(TargetPath))
            return;

        try
        {
            var isEmpty = await _pathService.IsDirectoryEmptyAsync(TargetPath);
            if (!isEmpty)
            {
                var analysis = await _pathService.AnalyzeDirectoryAsync(SourcePath ?? "", TargetPath);
                ShowLogsOption = analysis.HasLogsDirectory;
                ShowDataOption = analysis.HasDataDirectory;
                ShowDeploymentOption = analysis.HasDeploymentDirectory;

                if (ShowLogsOption || ShowDataOption || ShowDeploymentOption)
                {
                    var optionsText = "";
                    if (ShowLogsOption) optionsText += "logs directory";
                    if (ShowDataOption) optionsText += (ShowLogsOption ? " and " : "") + "data directory";
                    if (ShowDeploymentOption) optionsText += (ShowLogsOption || ShowDataOption ? " and " : "") + "existing deployment";
                    
                    WarningMessage = $"Target contains {optionsText}";
                }
                else
                {
                    WarningMessage = null;
                }
            }
            else
            {
                ShowLogsOption = false;
                ShowDataOption = false;
                ShowDeploymentOption = false;
                WarningMessage = null;
            }

            ProgressMessage = "✅ Target directory selected";
        }
        catch (Exception ex)
        {
            WarningMessage = $"Error validating target path: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes the installation process
    /// </summary>
    private async Task ExecuteInstallationAsync()
    {
        if (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(TargetPath))
            return;

        try
        {
            IsInstalling = true;
            WarningMessage = null;

            // Create deployment info
            var targetAnalysis = await _pathService.AnalyzeDirectoryAsync(SourcePath, TargetPath);
            DeploymentInfo = new DeploymentInfo
            {
                SourcePath = SourcePath,
                TargetPath = TargetPath,
                HasLogsDirectory = targetAnalysis.HasLogsDirectory,
                HasDataDirectory = targetAnalysis.HasDataDirectory
            };

            // Execute installation
            var success = await _fileOperationService.CopyDeploymentAsync(
                DeploymentInfo, 
                KeepLogs, 
                KeepData, 
                KeepDeployment,
                _progressReporter);

            if (success)
            {
                ProgressMessage = "🎉 Installation completed successfully!";
            }
            else
            {
                WarningMessage = "❌ Installation failed. Check the progress messages for details.";
            }
        }
        catch (Exception ex)
        {
            WarningMessage = $"Installation error: {ex.Message}";
            ProgressMessage = "❌ Installation failed";
        }
        finally
        {
            IsInstalling = false;
            ShowDataOption = false;
            ShowLogsOption = false;
            ShowDeploymentOption = false;
        }
    }

    /// <summary>
    /// Determines if installation can be executed
    /// </summary>
    private bool CanExecuteInstallation()
    {
        return !IsInstalling &&
               !string.IsNullOrEmpty(SourcePath) &&
               !string.IsNullOrEmpty(TargetPath);
    }

    /// <summary>
    /// Updates the state of the install button
    /// </summary>
    private void UpdateInstallButtonState()
    {
        ((AsyncRelayCommand)InstallCommand).NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when properties change to update UI state
    /// </summary>
    partial void OnSourcePathChanged(string? value)
    {
        UpdateInstallButtonState();
    }

    partial void OnTargetPathChanged(string? value)
    {
        UpdateInstallButtonState();
    }

    partial void OnWarningMessageChanged(string? value)
    {
        UpdateInstallButtonState();
    }

    partial void OnIsInstallingChanged(bool value)
    {
        UpdateInstallButtonState();
    }
}