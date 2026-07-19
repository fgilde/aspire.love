using AspireLove.Core.Naming;
using AspireLove.Core.Options;

namespace AspireLove.Core.Generation;

/// <summary>
/// Flattened, fully-resolved view of <see cref="GenerationOptions"/> that the Scriban
/// templates bind against. All naming/branching decisions are made here so the templates
/// stay declarative.
/// </summary>
public sealed class TemplateModel
{
    public TemplateModel(GenerationOptions options)
    {
        var projectName = options.ProjectName is { Length: > 0 }
            ? options.ProjectName
            : Resolution.OptionsResolver.FallbackProjectName;

        DisplayProjectName = projectName;
        ProjectIdentifier = NameConventions.ToPascalIdentifier(projectName);
        SolutionName = ProjectIdentifier;
        AppHostProjectName = $"{ProjectIdentifier}.AppHost";
        AppHostNamespace = AppHostProjectName;

        var slug = NameConventions.ToResourceSlug(projectName);
        SupabaseResourceName = $"{slug}-supabase";
        FrontendResourceName = $"{slug}-frontend";

        OrganizationName = options.OrganizationName;
        UserName = options.User.Name;
        UserEmail = options.User.Email;
        UserPassword = options.User.Password;

        LovableApiKey = options.LovableApiKey;
        HasLovableApiKey = !string.IsNullOrWhiteSpace(options.LovableApiKey);

        Mode = options.Mode.ToString();
        IsFullLocal = options.Mode == GenerationMode.FullLocal;
        IsSupabaseSync = options.Mode == GenerationMode.SupabaseSync;
        IsRemoteConnect = options.Mode == GenerationMode.RemoteConnect;
        UsesLocalSupabase = options.UsesLocalSupabase;

        AddMonitoring = options.AddMonitoring && options.UsesLocalSupabase;
        DatabasePassword = options.DatabasePassword;

        // Persistent storage only makes sense when we actually deploy a Supabase stack.
        AddPersistentStorage = options.AddPersistentStorage && options.UsesLocalSupabase;
        AddDeployScript = options.AddDeployScript;

        // Azure storage account names must be 3-24 chars, lowercase alphanumeric only.
        var storageStem = new string(slug.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (storageStem.Length == 0) storageStem = "aspirelove";
        NfsStorageName = (storageStem.Length > 21 ? storageStem[..21] : storageStem) + "nfs";
        StorageVnetName = $"{slug}-vnet";
        MinioRootUser = $"{slug}-minio-admin";
        MinioRootPassword = "Change-This-Minio-Password-2026!";

        if (options.SyncInfo is { } sync)
        {
            SyncProjectRef = sync.ProjectRef;
            SyncServiceKey = sync.ServiceKey;
            SyncDbPassword = sync.DbPassword;
            SyncManagementToken = sync.ManagementToken;
        }

        if (options.RemoteInfo is { } remote)
        {
            RemoteProjectRef = remote.ProjectRef;
            RemoteServiceKey = remote.ServiceKey;
        }

        UserSecretsId = Guid.NewGuid().ToString();
        GrafanaDashboardsFolder = ProjectIdentifier;
    }

    // Package versions used by the generated csproj. Bump in one place.
    public string AspireSdkVersion => "13.4.6";
    public string AspireVersion => "13.4.6";
    public string NextendedSupabaseVersion => "10.1.14";

    public string DisplayProjectName { get; }
    public string ProjectIdentifier { get; }
    public string SolutionName { get; }
    public string AppHostProjectName { get; }
    public string AppHostNamespace { get; }

    public string SupabaseResourceName { get; }
    public string FrontendResourceName { get; }

    public string OrganizationName { get; }
    public string UserName { get; }
    public string UserEmail { get; }
    public string UserPassword { get; }

    public string? LovableApiKey { get; }
    public bool HasLovableApiKey { get; }

    public string Mode { get; }
    public bool IsFullLocal { get; }
    public bool IsSupabaseSync { get; }
    public bool IsRemoteConnect { get; }
    public bool UsesLocalSupabase { get; }

    public bool AddMonitoring { get; }
    public string DatabasePassword { get; }

    public bool AddPersistentStorage { get; }
    public bool AddDeployScript { get; }
    public string NfsStorageName { get; }
    public string StorageVnetName { get; }
    public string MinioRootUser { get; }
    public string MinioRootPassword { get; }

    public string? SyncProjectRef { get; }
    public string? SyncServiceKey { get; }
    public string? SyncDbPassword { get; }
    public string? SyncManagementToken { get; }

    public string? RemoteProjectRef { get; }
    public string? RemoteServiceKey { get; }

    public string UserSecretsId { get; }
    public string GrafanaDashboardsFolder { get; }
}
