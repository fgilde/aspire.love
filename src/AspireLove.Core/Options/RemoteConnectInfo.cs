namespace AspireLove.Core.Options;

/// <summary>
/// Minimal credentials to reference an existing Supabase cloud project without running a
/// local stack (mirrors <c>ISupabaseReferenceInfo</c>).
/// </summary>
public sealed record RemoteConnectInfo(string ProjectRef, string ServiceKey);
