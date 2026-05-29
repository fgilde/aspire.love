namespace AspireLove.Core.Options;

/// <summary>
/// Credentials required to sync a local stack from an existing Supabase cloud project
/// (mirrors <c>ISupabaseFullSyncInfo</c> from Nextended.Aspire.Hosting.Supabase).
/// </summary>
public sealed record SupabaseSyncInfo(
    string ProjectRef,
    string ServiceKey,
    string DbPassword,
    string ManagementToken);
