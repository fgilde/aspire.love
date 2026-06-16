using AspireLove.Core;
using AspireLove.Core.Options;

namespace AspireLove.Core.Tests;

public class ExistingProjectReaderTests
{
    private readonly AspireLoveGenerator _generator = new();

    /// <summary>Generates a project for the given options, then reads it back.</summary>
    private GenerationOptions RoundTrip(TempProject project, GenerationOptions options)
    {
        _generator.Run(options);
        Assert.True(ExistingProjectReader.TryRead(project.Path, out var read));
        return read;
    }

    [Fact]
    public void Returns_false_when_no_aspire_folder()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        Assert.False(ExistingProjectReader.TryRead(project.Path, out _));
    }

    [Fact]
    public void Round_trips_full_local_with_all_options()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var read = RoundTrip(project, new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "My Shop",
            OrganizationName = "Acme Inc",
            User = new DefaultUser("root", "root@example.com", "s3cret"),
            DatabasePassword = "pg-pw-42",
            LovableApiKey = "sk-lov-abc",
            Mode = GenerationMode.FullLocal,
            AddMonitoring = true,
            AddPersistentStorage = true,
            AddDeployScript = true,
        });

        Assert.Equal("My Shop", read.ProjectName);
        Assert.Equal("Acme Inc", read.OrganizationName);
        Assert.Equal("root", read.User.Name);
        Assert.Equal("root@example.com", read.User.Email);
        Assert.Equal("s3cret", read.User.Password);
        Assert.Equal("pg-pw-42", read.DatabasePassword);
        Assert.Equal("sk-lov-abc", read.LovableApiKey);
        Assert.Equal(GenerationMode.FullLocal, read.Mode);
        Assert.True(read.AddMonitoring);
        Assert.True(read.AddPersistentStorage);
        Assert.True(read.AddDeployScript);
    }

    [Fact]
    public void Round_trips_full_local_with_no_optional_flags()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var read = RoundTrip(project, new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "Plain",
            Mode = GenerationMode.FullLocal,
        });

        Assert.Equal(GenerationMode.FullLocal, read.Mode);
        Assert.False(read.AddMonitoring);
        Assert.False(read.AddPersistentStorage);
        Assert.False(read.AddDeployScript);
        Assert.Null(read.LovableApiKey);
    }

    [Fact]
    public void Round_trips_supabase_sync_credentials()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var read = RoundTrip(project, new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "Synced",
            Mode = GenerationMode.SupabaseSync,
            SyncInfo = new SupabaseSyncInfo("proj-ref", "svc-key", "db-pw", "mgmt-token"),
        });

        Assert.Equal(GenerationMode.SupabaseSync, read.Mode);
        Assert.NotNull(read.SyncInfo);
        Assert.Equal("proj-ref", read.SyncInfo!.ProjectRef);
        Assert.Equal("svc-key", read.SyncInfo.ServiceKey);
        Assert.Equal("db-pw", read.SyncInfo.DbPassword);
        Assert.Equal("mgmt-token", read.SyncInfo.ManagementToken);
    }

    [Fact]
    public void Round_trips_remote_connect_credentials()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var read = RoundTrip(project, new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "Remote",
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("remote-ref", "remote-key"),
        });

        Assert.Equal(GenerationMode.RemoteConnect, read.Mode);
        Assert.NotNull(read.RemoteInfo);
        Assert.Equal("remote-ref", read.RemoteInfo!.ProjectRef);
        Assert.Equal("remote-key", read.RemoteInfo.ServiceKey);
        // ProjectName falls back to the AppHost folder identifier for remote mode.
        Assert.False(string.IsNullOrWhiteSpace(read.ProjectName));
    }
}
