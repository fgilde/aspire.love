namespace AspireLove.Core.Options;

/// <summary>
/// Determines how the generated AppHost talks to Supabase.
/// </summary>
public enum GenerationMode
{
    /// <summary>
    /// Runs the full Supabase stack locally in Docker and applies the Lovable
    /// project's migrations and edge functions. The default and most self-contained mode.
    /// </summary>
    FullLocal,

    /// <summary>
    /// Runs the stack locally but syncs schema/data from an existing Supabase cloud
    /// project via <c>WithProjectSync</c>. Requires <see cref="GenerationOptions.SyncInfo"/>.
    /// </summary>
    SupabaseSync,

    /// <summary>
    /// Does not run a local stack; the app references an existing Supabase cloud project
    /// directly (the <c>addSupabase = false</c> case). Requires <see cref="GenerationOptions.RemoteInfo"/>.
    /// </summary>
    RemoteConnect,
}
