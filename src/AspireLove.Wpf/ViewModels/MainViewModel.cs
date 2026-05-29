using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using AspireLove.Core;
using AspireLove.Core.Generation;
using AspireLove.Core.Options;
using AspireLove.Core.Resolution;
using AspireLove.Core.Validation;
using AspireLove.Studio.Mvvm;
using Microsoft.Win32;

namespace AspireLove.Studio.ViewModels;

/// <summary>
/// Drives the whole window. Holds the user's inputs, runs the same Core validation/generation
/// the CLI uses (never shells out), and exposes live validation plus a code preview.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly AspireLoveGenerator _generator = new();

    // Coalesces rapid keystrokes so we re-render the preview once typing settles, not per character.
    private readonly DispatcherTimer _previewDebounce;

    public MainViewModel()
    {
        _previewDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _previewDebounce.Tick += (_, _) =>
        {
            _previewDebounce.Stop();
            RefreshPreview();
        };

        BrowseCommand = new RelayCommand(Browse);
        GenerateCommand = new RelayCommand(Generate, () => IsValid && !DryRun);
        LaunchCommand = new RelayCommand(Launch, () => AspireProjectExists);
        PublishCommand = new RelayCommand(Publish, () => AspireProjectExists);
        Revalidate();
        RefreshPreview();
        UpdateAspireProjectState();
    }

    // ---- Inputs --------------------------------------------------------------

    private string _lovableProjectPath = "";
    public string LovableProjectPath
    {
        get => _lovableProjectPath;
        set { if (SetField(ref _lovableProjectPath, value)) OnInputChanged(resolveName: true); }
    }

    private string _projectName = "";
    public string ProjectName
    {
        get => _projectName;
        set { if (SetField(ref _projectName, value)) OnInputChanged(); }
    }

    private string _organizationName = "My Company";
    public string OrganizationName
    {
        get => _organizationName;
        set { if (SetField(ref _organizationName, value)) OnInputChanged(); }
    }

    private string _userName = "admin";
    public string UserName { get => _userName; set { if (SetField(ref _userName, value)) OnInputChanged(); } }

    private string _userEmail = "admin@localhost";
    public string UserEmail { get => _userEmail; set { if (SetField(ref _userEmail, value)) OnInputChanged(); } }

    private string _userPassword = "admin";
    public string UserPassword { get => _userPassword; set { if (SetField(ref _userPassword, value)) OnInputChanged(); } }

    private string _databasePassword = "local-dev-password-123";
    public string DatabasePassword { get => _databasePassword; set { if (SetField(ref _databasePassword, value)) OnInputChanged(); } }

    private string _lovableApiKey = "";
    public string LovableApiKey { get => _lovableApiKey; set { if (SetField(ref _lovableApiKey, value)) OnInputChanged(); } }

    public IReadOnlyList<GenerationMode> Modes { get; } =
        Enum.GetValues<GenerationMode>().ToArray();

    private GenerationMode _mode = GenerationMode.FullLocal;
    public GenerationMode Mode
    {
        get => _mode;
        set
        {
            if (!SetField(ref _mode, value))
                return;

            // Monitoring is only meaningful with a local stack — clear it otherwise.
            if (!UsesLocalSupabase && AddMonitoring)
                _addMonitoring = false;

            OnPropertyChanged(nameof(UsesLocalSupabase));
            OnPropertyChanged(nameof(IsSyncMode));
            OnPropertyChanged(nameof(IsRemoteMode));
            OnPropertyChanged(nameof(AddMonitoring));
            OnPropertyChanged(nameof(ModeDescription));
            OnInputChanged();
        }
    }

    private bool _addMonitoring;
    public bool AddMonitoring
    {
        get => _addMonitoring;
        set { if (SetField(ref _addMonitoring, value)) OnInputChanged(); }
    }

    private bool _dryRun;
    public bool DryRun
    {
        get => _dryRun;
        set { if (SetField(ref _dryRun, value)) { GenerateCommand.RaiseCanExecuteChanged(); } }
    }

    // Supabase Sync credentials.
    private string _syncProjectRef = "";
    public string SyncProjectRef { get => _syncProjectRef; set { if (SetField(ref _syncProjectRef, value)) OnInputChanged(); } }
    private string _syncServiceKey = "";
    public string SyncServiceKey { get => _syncServiceKey; set { if (SetField(ref _syncServiceKey, value)) OnInputChanged(); } }
    private string _syncDbPassword = "";
    public string SyncDbPassword { get => _syncDbPassword; set { if (SetField(ref _syncDbPassword, value)) OnInputChanged(); } }
    private string _syncManagementToken = "";
    public string SyncManagementToken { get => _syncManagementToken; set { if (SetField(ref _syncManagementToken, value)) OnInputChanged(); } }

    // Remote Connect credentials.
    private string _remoteProjectRef = "";
    public string RemoteProjectRef { get => _remoteProjectRef; set { if (SetField(ref _remoteProjectRef, value)) OnInputChanged(); } }
    private string _remoteServiceKey = "";
    public string RemoteServiceKey { get => _remoteServiceKey; set { if (SetField(ref _remoteServiceKey, value)) OnInputChanged(); } }

    // ---- Derived view state --------------------------------------------------

    public bool UsesLocalSupabase => Mode is GenerationMode.FullLocal or GenerationMode.SupabaseSync;
    public bool IsSyncMode => Mode == GenerationMode.SupabaseSync;
    public bool IsRemoteMode => Mode == GenerationMode.RemoteConnect;

    public string ModeDescription => Mode switch
    {
        GenerationMode.FullLocal =>
            "Runs the full Supabase stack locally and applies this project's migrations and edge functions.",
        GenerationMode.SupabaseSync =>
            "Runs Supabase locally but pulls schema and data from an existing cloud project.",
        GenerationMode.RemoteConnect =>
            "No local stack — the frontend talks directly to an existing Supabase cloud project.",
        _ => "",
    };

    private bool _isValid;
    public bool IsValid { get => _isValid; private set => SetField(ref _isValid, value); }

    private string _validationSummary = "";
    public string ValidationSummary { get => _validationSummary; private set => SetField(ref _validationSummary, value); }

    private bool _hasWarnings;
    public bool HasWarnings { get => _hasWarnings; private set => SetField(ref _hasWarnings, value); }

    private string _resolvedNameHint = "";
    public string ResolvedNameHint { get => _resolvedNameHint; private set => SetField(ref _resolvedNameHint, value); }

    private string _previewContent = "";
    public string PreviewContent { get => _previewContent; private set => SetField(ref _previewContent, value); }

    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    // True once an aspire folder with an AppHost project exists at the chosen path.
    private AspireProjectInfo? _aspireProject;
    private bool _aspireProjectExists;
    public bool AspireProjectExists
    {
        get => _aspireProjectExists;
        private set
        {
            if (!SetField(ref _aspireProjectExists, value))
                return;
            LaunchCommand.RaiseCanExecuteChanged();
            PublishCommand.RaiseCanExecuteChanged();
        }
    }

    private string _aspireProjectStatus = "";
    public string AspireProjectStatus { get => _aspireProjectStatus; private set => SetField(ref _aspireProjectStatus, value); }

    // ---- Commands ------------------------------------------------------------

    public RelayCommand BrowseCommand { get; }
    public RelayCommand GenerateCommand { get; }
    public RelayCommand LaunchCommand { get; }
    public RelayCommand PublishCommand { get; }

    private void Browse()
    {
        var dialog = new OpenFolderDialog { Title = "Select your Lovable project folder" };
        if (dialog.ShowDialog() == true)
            LovableProjectPath = dialog.FolderName;
    }

    /// <summary>
    /// Re-renders the AppHost.cs preview from the current inputs. Runs automatically (debounced)
    /// on every change, so the user never has to ask for it.
    /// </summary>
    private void RefreshPreview()
    {
        if (!IsValid)
        {
            PreviewContent = "// Fix the validation issues above to see the generated AppHost.cs.";
            return;
        }

        try
        {
            var files = _generator.Preview(BuildOptions());
            var appHost = files.FirstOrDefault(f => f.RelativePath.EndsWith("AppHost.cs"));
            PreviewContent = appHost?.Content ?? "(no AppHost.cs generated)";
        }
        catch (Exception ex)
        {
            PreviewContent = $"// Preview failed: {ex.Message}";
        }
    }

    private void Generate()
    {
        try
        {
            var outcome = _generator.Run(BuildOptions());
            var pkg = outcome.PackageJson switch
            {
                PackageJsonUpdateOutcome.Added => " package.json updated with the 'aspire' script.",
                PackageJsonUpdateOutcome.AlreadyPresent => " ('aspire' script was already present.)",
                PackageJsonUpdateOutcome.NotFound => " (no package.json found to update.)",
                _ => "",
            };
            StatusMessage = $"Generated {outcome.Files.Count} files into {outcome.OutputRoot}.{pkg}";
            UpdateAspireProjectState();
        }
        catch (GenerationValidationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation failed: {ex.Message}";
        }
    }

    private void Launch()
    {
        if (_aspireProject is not { } project)
            return;

        // `dotnet run` brings up the Aspire AppHost and its dashboard; keep the console open
        // so the user can read the dashboard URL and logs.
        if (OpenTerminal(project.AppHostDirectory, "dotnet run"))
            StatusMessage = "Launching the Aspire AppHost in a new terminal…";
    }

    private void Publish()
    {
        if (_aspireProject is not { } project)
            return;

        // `azd up` provisions and deploys to Azure Container Apps. It is interactive (subscription,
        // location, confirmation prompts), so it must run in a visible terminal.
        if (OpenTerminal(project.AppHostDirectory, "azd up"))
            StatusMessage = "Publishing with 'azd up' in a new terminal…";
    }

    private bool OpenTerminal(string workingDirectory, string command)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {command}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not start '{command}': {ex.Message}";
            return false;
        }
    }

    private void UpdateAspireProjectState()
    {
        _aspireProject = AspireProjectLocator.Locate(LovableProjectPath);
        AspireProjectExists = _aspireProject is not null;
        AspireProjectStatus = _aspireProject is { } p
            ? $"Aspire project detected — {Path.GetFileName(p.AppHostDirectory)}"
            : "No aspire project here yet. Generate one to enable launch & publish.";
    }

    // ---- Plumbing ------------------------------------------------------------

    private void OnInputChanged(bool resolveName = false)
    {
        if (resolveName)
        {
            UpdateResolvedNameHint();
            UpdateAspireProjectState();
        }

        Revalidate();

        // Restart the debounce window — the preview refreshes once edits settle.
        _previewDebounce.Stop();
        _previewDebounce.Start();
    }

    private void UpdateResolvedNameHint()
    {
        if (!string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(LovableProjectPath))
        {
            ResolvedNameHint = "";
            return;
        }

        try
        {
            var resolved = _generator.Resolve(BuildOptions());
            ResolvedNameHint = $"Resolved project name: {resolved.ProjectName}";
        }
        catch
        {
            ResolvedNameHint = "";
        }
    }

    private void Revalidate()
    {
        var result = _generator.Validate(BuildOptions());
        IsValid = result.IsValid;
        HasWarnings = result.HasWarnings;

        var lines = result.Errors.Select(e => "✗ " + e.Text)
            .Concat(result.Warnings.Select(w => "⚠ " + w.Text))
            .ToArray();
        ValidationSummary = lines.Length == 0 ? "All checks passed." : string.Join(Environment.NewLine, lines);

        GenerateCommand.RaiseCanExecuteChanged();
    }

    private GenerationOptions BuildOptions() => new()
    {
        LovableProjectPath = LovableProjectPath,
        ProjectName = string.IsNullOrWhiteSpace(ProjectName) ? null : ProjectName,
        OrganizationName = OrganizationName,
        User = new DefaultUser(UserName, UserEmail, UserPassword),
        LovableApiKey = string.IsNullOrWhiteSpace(LovableApiKey) ? null : LovableApiKey,
        Mode = Mode,
        AddMonitoring = AddMonitoring,
        DatabasePassword = DatabasePassword,
        SyncInfo = IsSyncMode
            ? new SupabaseSyncInfo(SyncProjectRef, SyncServiceKey, SyncDbPassword, SyncManagementToken)
            : null,
        RemoteInfo = IsRemoteMode
            ? new RemoteConnectInfo(RemoteProjectRef, RemoteServiceKey)
            : null,
        DryRun = DryRun,
    };
}
