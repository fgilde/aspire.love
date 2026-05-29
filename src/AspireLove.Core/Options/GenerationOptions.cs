namespace AspireLove.Core.Options;

/// <summary>
/// The full set of inputs that drive code generation. Create one of these directly or let
/// <see cref="Resolution.OptionsResolver"/> fill in the defaults from the Lovable project.
/// </summary>
public sealed record GenerationOptions
{
    /// <summary>Absolute path to the root of the Lovable project (where package.json lives).</summary>
    public required string LovableProjectPath { get; init; }

    /// <summary>
    /// Display/base name for the generated solution and AppHost project. When null or empty
    /// the resolver derives it from package.json, falling back to "MyAspireLove".
    /// </summary>
    public string? ProjectName { get; init; }

    public string OrganizationName { get; init; } = "My Company";

    public DefaultUser User { get; init; } = DefaultUser.CreateDefault();

    /// <summary>
    /// Optional Lovable AI gateway key. When set it is wired into the edge runtime so the
    /// project's built-in Lovable AI features keep working locally.
    /// </summary>
    public string? LovableApiKey { get; init; }

    public GenerationMode Mode { get; init; } = GenerationMode.FullLocal;

    /// <summary>Adds the Grafana/Tempo/OTel observability stack. Only valid with a local stack.</summary>
    public bool AddMonitoring { get; init; }

    /// <summary>Password for the local Postgres database (local modes only).</summary>
    public string DatabasePassword { get; init; } = "local-dev-password-123";

    /// <summary>Required when <see cref="Mode"/> is <see cref="GenerationMode.SupabaseSync"/>.</summary>
    public SupabaseSyncInfo? SyncInfo { get; init; }

    /// <summary>Required when <see cref="Mode"/> is <see cref="GenerationMode.RemoteConnect"/>.</summary>
    public RemoteConnectInfo? RemoteInfo { get; init; }

    /// <summary>When true the generator does not write any files (used for previews / --dry-run).</summary>
    public bool DryRun { get; init; }

    /// <summary>True for the two modes that run a local Supabase stack.</summary>
    public bool UsesLocalSupabase => Mode is GenerationMode.FullLocal or GenerationMode.SupabaseSync;
}
