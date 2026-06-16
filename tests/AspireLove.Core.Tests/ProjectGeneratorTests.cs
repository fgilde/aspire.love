using AspireLove.Core.Generation;
using AspireLove.Core.Options;

namespace AspireLove.Core.Tests;

public class ProjectGeneratorTests
{
    private readonly ProjectGenerator _generator = new();

    private static GenerationOptions FullLocal(string path) => new()
    {
        LovableProjectPath = path,
        ProjectName = "My Cool App",
    };

    private static string AppHostCs(IReadOnlyList<GeneratedFile> files) =>
        files.Single(f => f.RelativePath.EndsWith("AppHost.cs")).Content;

    private static string ConstantsCs(IReadOnlyList<GeneratedFile> files) =>
        files.Single(f => f.RelativePath.EndsWith("Constants.cs")).Content;

    private static string Csproj(IReadOnlyList<GeneratedFile> files) =>
        files.Single(f => f.RelativePath.EndsWith(".csproj")).Content;

    /// <summary>Drops comment lines so negative assertions ignore the "to switch modes later"
    /// hints that intentionally name the other mode's method.</summary>
    private static string CodeOnly(string content) =>
        string.Join('\n', content.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));

    [Fact]
    public void Generates_expected_file_set_for_full_local()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(FullLocal(project.Path));

        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.slnx");
        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.AppHost/MyCoolApp.AppHost.csproj");
        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.AppHost/AppHost.cs");
        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.AppHost/Constants.cs");
        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.AppHost/appsettings.json");
        Assert.Contains(files, f => f.RelativePath == "MyCoolApp.AppHost/Properties/launchSettings.json");
    }

    [Fact]
    public void Never_emits_reference_project_specific_artifacts()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(FullLocal(project.Path));

        // The "way2ofd" Docker bits and the local Extensions folder belong to the reference
        // project / the NuGet package — they must never leak into generated output.
        Assert.DoesNotContain(files, f => f.RelativePath.Contains("way2ofd", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, f => f.RelativePath.Contains("Extensions", StringComparison.OrdinalIgnoreCase));
        Assert.All(files, f => Assert.DoesNotContain("way2ofd", f.Content));
    }

    [Fact]
    public void Full_local_uses_migrations_and_edge_functions()
    {
        using var project = new TempProject().WithPackageName("app");

        var appHost = AppHostCs(_generator.Generate(FullLocal(project.Path)));

        Assert.Contains(".WithMigrations(", appHost);
        Assert.Contains(".WithEdgeFunctions(", appHost);
        Assert.DoesNotContain(".WithProjectSync(", CodeOnly(appHost));
    }

    [Fact]
    public void Supabase_sync_uses_project_sync_not_migrations()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
            Mode = GenerationMode.SupabaseSync,
            SyncInfo = new SupabaseSyncInfo("ref123", "service-key", "db-pw", "mgmt-token"),
        });

        var appHost = AppHostCs(files);
        Assert.Contains(".WithProjectSync(Constants.Supabase.SyncInfo)", appHost);
        Assert.DoesNotContain(".WithMigrations(", CodeOnly(appHost));

        var constants = ConstantsCs(files);
        Assert.Contains("ref123", constants);
        Assert.Contains("mgmt-token", constants);
        Assert.Contains("ISupabaseFullSyncInfo", constants);
    }

    [Fact]
    public void Remote_connect_skips_local_supabase_stack()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("remote-ref", "remote-key"),
        });

        var appHost = AppHostCs(files);
        Assert.DoesNotContain("builder.AddSupabase(", appHost);
        Assert.Contains(".WithSupabaseVite(Constants.Supabase.RemoteInfo)", appHost);

        var constants = ConstantsCs(files);
        Assert.Contains("remote-ref", constants);
        Assert.Contains("ISupabaseReferenceInfo", constants);
    }

    [Fact]
    public void Azure_publish_block_is_always_present()
    {
        using var project = new TempProject().WithPackageName("app");

        foreach (var mode in new[] { GenerationMode.FullLocal, GenerationMode.RemoteConnect })
        {
            var options = mode == GenerationMode.RemoteConnect
                ? new GenerationOptions
                {
                    LovableProjectPath = project.Path,
                    ProjectName = "App",
                    Mode = mode,
                    RemoteInfo = new RemoteConnectInfo("r", "k"),
                }
                : FullLocal(project.Path);

            var appHost = AppHostCs(_generator.Generate(options));
            Assert.Contains("builder.ExecutionContext.IsPublishMode", appHost);
            Assert.Contains("AddAzureContainerAppEnvironment", appHost);
        }
    }

    [Fact]
    public void Lovable_api_key_is_wired_only_when_provided()
    {
        using var project = new TempProject().WithPackageName("app");

        var without = AppHostCs(_generator.Generate(FullLocal(project.Path)));
        Assert.DoesNotContain("LOVABLE_API_KEY", without);

        var with = AppHostCs(_generator.Generate(FullLocal(project.Path) with { LovableApiKey = "sk-test" }));
        Assert.Contains("LOVABLE_API_KEY", with);
    }

    [Fact]
    public void Monitoring_adds_observability_block_and_gitkeep()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(FullLocal(project.Path) with { AddMonitoring = true });

        Assert.Contains("AddObservabilityStack", AppHostCs(files));
        Assert.Contains(files, f => f.RelativePath == "observability/grafana/dashboards/.gitkeep");
    }

    [Fact]
    public void Monitoring_is_dropped_when_not_local_supabase()
    {
        using var project = new TempProject().WithPackageName("app");

        // AddMonitoring requested but mode is remote → TemplateModel must drop it.
        var files = _generator.Generate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("r", "k"),
            AddMonitoring = true,
        });

        Assert.DoesNotContain("AddObservabilityStack", AppHostCs(files));
        Assert.DoesNotContain(files, f => f.RelativePath.Contains("observability"));
    }

    [Fact]
    public void Persistent_storage_wires_nfs_minio_and_constants_when_enabled()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(FullLocal(project.Path) with { AddPersistentStorage = true });

        var appHost = AppHostCs(files);
        Assert.Contains("var containerEnv = builder.AddAzureContainerAppEnvironment", appHost);
        Assert.Contains("AddSupabaseNfsStorage(containerEnv", appHost);
        Assert.Contains("AddMinioS3OnNfs(", appHost);

        var constants = ConstantsCs(files);
        Assert.Contains("class Storage", constants);
        Assert.Contains("MinioRootPassword", constants);
    }

    [Fact]
    public void Persistent_storage_absent_leaves_no_unused_container_env_variable()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(FullLocal(project.Path));

        var appHost = AppHostCs(files);
        Assert.DoesNotContain("var containerEnv", appHost);
        Assert.DoesNotContain("AddSupabaseNfsStorage", appHost);
        Assert.DoesNotContain("class Storage", ConstantsCs(files));
    }

    [Fact]
    public void Persistent_storage_is_dropped_when_not_local_supabase()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _generator.Generate(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
            Mode = GenerationMode.RemoteConnect,
            RemoteInfo = new RemoteConnectInfo("r", "k"),
            AddPersistentStorage = true,
        });

        Assert.DoesNotContain("AddSupabaseNfsStorage", AppHostCs(files));
        Assert.DoesNotContain("class Storage", ConstantsCs(files));
    }

    [Fact]
    public void Deploy_script_is_generated_only_when_requested()
    {
        using var project = new TempProject().WithPackageName("app");

        var without = _generator.Generate(FullLocal(project.Path));
        Assert.DoesNotContain(without, f => f.RelativePath == "scripts/deploy.ps1");

        var with = _generator.Generate(FullLocal(project.Path) with { AddDeployScript = true });
        var script = with.Single(f => f.RelativePath == "scripts/deploy.ps1").Content;
        // The script must target this project's AppHost directory and call azd.
        Assert.Contains("MyCoolApp.AppHost", script);
        Assert.Contains("azd up", script);
        Assert.DoesNotContain("way2ofd", script);
    }

    [Fact]
    public void Deploy_script_is_pure_ascii()
    {
        using var project = new TempProject().WithPackageName("app");

        var script = _generator.Generate(FullLocal(project.Path) with { AddDeployScript = true })
            .Single(f => f.RelativePath == "scripts/deploy.ps1").Content;

        // A non-ASCII char (e.g. an em dash) in a .ps1 breaks Windows PowerShell 5.1 parsing when
        // the file is read without a BOM — keep the script ASCII so it's safe regardless.
        var offender = script.FirstOrDefault(c => c > '\x7F');
        Assert.True(offender == default, $"deploy.ps1 contains a non-ASCII character: U+{(int)offender:X4}");
    }

    [Fact]
    public void Csproj_pins_expected_package_versions()
    {
        using var project = new TempProject().WithPackageName("app");

        var csproj = Csproj(_generator.Generate(FullLocal(project.Path)));

        Assert.Contains("Aspire.AppHost.Sdk", csproj);
        Assert.Contains("Nextended.Aspire.Hosting.Supabase", csproj);
        Assert.Contains("net10.0", csproj);
    }

    [Fact]
    public void No_triple_blank_lines_in_generated_csharp()
    {
        using var project = new TempProject().WithPackageName("app");

        var appHost = AppHostCs(_generator.Generate(FullLocal(project.Path)));

        Assert.DoesNotContain("\n\n\n", appHost.Replace("\r", ""));
    }
}
