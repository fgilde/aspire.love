using AspireLove.Core.Options;
using AspireLove.Core.Validation;

namespace AspireLove.Core.Tests;

public class GenerationValidatorTests
{
    [Fact]
    public void Missing_path_is_an_error()
    {
        var result = GenerationValidator.Validate(new GenerationOptions { LovableProjectPath = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, m => m.Text.Contains("path"));
    }

    [Fact]
    public void Nonexistent_path_is_an_error()
    {
        var path = Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N"));

        var result = GenerationValidator.Validate(new GenerationOptions { LovableProjectPath = path });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Full_local_with_complete_layout_is_valid()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.FullLocal,
        });

        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Missing_package_json_is_a_warning_not_an_error()
    {
        using var project = new TempProject().WithSupabaseLayout();

        var result = GenerationValidator.Validate(new GenerationOptions { LovableProjectPath = project.Path });

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, m => m.Text.Contains("package.json"));
    }

    [Fact]
    public void Full_local_without_migrations_folder_warns()
    {
        using var project = new TempProject().WithPackageName("app");
        Directory.CreateDirectory(Path.Combine(project.Path, "supabase"));

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.FullLocal,
        });

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, m => m.Text.Contains("migrations"));
        Assert.Contains(result.Warnings, m => m.Text.Contains("functions"));
    }

    [Fact]
    public void Supabase_sync_without_credentials_is_an_error()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.SupabaseSync,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Supabase_sync_with_blank_field_is_an_error()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.SupabaseSync,
            SyncInfo = new SupabaseSyncInfo("ref", "key", "", "token"),
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, m => m.Text.Contains("Database Password"));
    }

    [Fact]
    public void Supabase_sync_with_full_credentials_is_valid()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.SupabaseSync,
            SyncInfo = new SupabaseSyncInfo("ref", "key", "pw", "token"),
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Remote_connect_without_credentials_is_an_error()
    {
        using var project = new TempProject().WithPackageName("app");

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.RemoteConnect,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Remote_connect_with_credentials_is_valid()
    {
        using var project = new TempProject().WithPackageName("app");

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("ref", "key"),
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Monitoring_with_remote_connect_is_an_error()
    {
        using var project = new TempProject().WithPackageName("app");

        var result = GenerationValidator.Validate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("ref", "key"),
            AddMonitoring = true,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, m => m.Text.Contains("Monitoring"));
    }

    [Fact]
    public void Existing_aspire_folder_warns_about_overwrite()
    {
        using var project = new TempProject().WithPackageName("app").WithSupabaseLayout();
        Directory.CreateDirectory(Path.Combine(project.Path, "aspire"));

        var result = GenerationValidator.Validate(new GenerationOptions { LovableProjectPath = project.Path });

        Assert.Contains(result.Warnings, m => m.Text.Contains("already exists"));
    }
}
