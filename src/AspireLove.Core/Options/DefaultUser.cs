namespace AspireLove.Core.Options;

/// <summary>
/// The admin user that gets seeded into the local Supabase auth stack and used as the
/// Studio / Grafana login.
/// </summary>
public sealed record DefaultUser(string Name, string Email, string Password)
{
    public static DefaultUser CreateDefault() => new("admin", "admin@localhost", "admin");
}
