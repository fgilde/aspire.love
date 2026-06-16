using System.CommandLine;
using AspireLove.Cli;
using AspireLove.Core;
using AspireLove.Core.Options;

var pathOption = new Option<string>("--path", "-p")
{
    Description = "Path to the root of the Lovable project.",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
};
var nameOption = new Option<string?>("--name", "-n")
{
    Description = "Project name. Defaults to the package.json name, then 'MyAspireLove'.",
};
var orgOption = new Option<string>("--organization", "-o")
{
    Description = "Organization name shown in Supabase Studio.",
    DefaultValueFactory = _ => "My Company",
};
var modeOption = new Option<GenerationMode>("--mode", "-m")
{
    Description = "Supabase mode: FullLocal, SupabaseSync or RemoteConnect.",
    DefaultValueFactory = _ => GenerationMode.FullLocal,
};
var monitoringOption = new Option<bool>("--monitoring")
{
    Description = "Add the Grafana/Tempo/OpenTelemetry observability stack (local modes only).",
};
var persistentStorageOption = new Option<bool>("--persistent-storage")
{
    Description = "Persist Supabase Storage when deployed (Azure Files NFS + MinIO S3; local modes only).",
};
var deployScriptOption = new Option<bool>("--deploy-script")
{
    Description = "Generate a guided scripts/deploy.ps1 for deploying to Azure with azd.",
};
var lovableKeyOption = new Option<string?>("--lovable-api-key")
{
    Description = "Lovable AI gateway key, so the project's built-in AI keeps working locally.",
};
var dbPasswordOption = new Option<string>("--db-password")
{
    Description = "Local Postgres password.",
    DefaultValueFactory = _ => "local-dev-password-123",
};

var userNameOption = new Option<string>("--user-name")
{
    Description = "Default admin user name.",
    DefaultValueFactory = _ => "admin",
};
var userEmailOption = new Option<string>("--user-email")
{
    Description = "Default admin user email.",
    DefaultValueFactory = _ => "admin@localhost",
};
var userPasswordOption = new Option<string>("--user-password")
{
    Description = "Default admin user password.",
    DefaultValueFactory = _ => "admin",
};

var syncProjectRefOption = new Option<string?>("--sync-project-ref") { Description = "Supabase project ref to sync from (SupabaseSync mode)." };
var syncServiceKeyOption = new Option<string?>("--sync-service-key") { Description = "Service key for sync (SupabaseSync mode)." };
var syncDbPasswordOption = new Option<string?>("--sync-db-password") { Description = "Database password for sync (SupabaseSync mode)." };
var syncManagementTokenOption = new Option<string?>("--sync-management-token") { Description = "Management token for sync (SupabaseSync mode)." };

var remoteProjectRefOption = new Option<string?>("--remote-project-ref") { Description = "Supabase project ref to connect to (RemoteConnect mode)." };
var remoteServiceKeyOption = new Option<string?>("--remote-service-key") { Description = "Service key (RemoteConnect mode)." };

var dryRunOption = new Option<bool>("--dry-run") { Description = "Show what would be generated without writing any files." };
var yesOption = new Option<bool>("--yes", "-y") { Description = "Proceed even if there are warnings." };

var initCommand = new Command("init", "Generate an Aspire AppHost into an existing Lovable project.")
{
    pathOption, nameOption, orgOption, modeOption, monitoringOption, persistentStorageOption, deployScriptOption,
    lovableKeyOption, dbPasswordOption,
    userNameOption, userEmailOption, userPasswordOption,
    syncProjectRefOption, syncServiceKeyOption, syncDbPasswordOption, syncManagementTokenOption,
    remoteProjectRefOption, remoteServiceKeyOption,
    dryRunOption, yesOption,
};

initCommand.SetAction(parseResult =>
{
    var mode = parseResult.GetValue(modeOption);

    var options = new GenerationOptions
    {
        LovableProjectPath = Path.GetFullPath(parseResult.GetValue(pathOption)!),
        ProjectName = parseResult.GetValue(nameOption),
        OrganizationName = parseResult.GetValue(orgOption)!,
        Mode = mode,
        AddMonitoring = parseResult.GetValue(monitoringOption),
        AddPersistentStorage = parseResult.GetValue(persistentStorageOption),
        AddDeployScript = parseResult.GetValue(deployScriptOption),
        LovableApiKey = parseResult.GetValue(lovableKeyOption),
        DatabasePassword = parseResult.GetValue(dbPasswordOption)!,
        User = new DefaultUser(
            parseResult.GetValue(userNameOption)!,
            parseResult.GetValue(userEmailOption)!,
            parseResult.GetValue(userPasswordOption)!),
        SyncInfo = mode == GenerationMode.SupabaseSync
            ? new SupabaseSyncInfo(
                parseResult.GetValue(syncProjectRefOption) ?? "",
                parseResult.GetValue(syncServiceKeyOption) ?? "",
                parseResult.GetValue(syncDbPasswordOption) ?? "",
                parseResult.GetValue(syncManagementTokenOption) ?? "")
            : null,
        RemoteInfo = mode == GenerationMode.RemoteConnect
            ? new RemoteConnectInfo(
                parseResult.GetValue(remoteProjectRefOption) ?? "",
                parseResult.GetValue(remoteServiceKeyOption) ?? "")
            : null,
        DryRun = parseResult.GetValue(dryRunOption),
    };

    return RunInit(options, skipWarnings: parseResult.GetValue(yesOption));
});

var updateCommand = new Command("update", "Check for and install the latest version of aspire.love.");
updateCommand.SetAction((_, cancellationToken) => UpdateRunner.RunAsync(cancellationToken));

var rootCommand = new RootCommand("aspire.love — make Lovable projects independent of the Lovable/Supabase cloud.")
{
    initCommand,
    updateCommand,
};

return await rootCommand.Parse(args).InvokeAsync();

static int RunInit(GenerationOptions options, bool skipWarnings)
{
    var generator = new AspireLoveGenerator();
    var resolved = generator.Resolve(options);

    ConsoleReporter.WriteLine(ConsoleColor.Cyan,
        $"aspire.love — project '{resolved.ProjectName}', mode {resolved.Mode}");

    var validation = generator.Validate(resolved);
    ConsoleReporter.PrintValidation(validation);

    if (!validation.IsValid)
    {
        ConsoleReporter.WriteLine(ConsoleColor.Red, "Aborted: please fix the errors above.");
        return 1;
    }

    if (validation.HasWarnings && !skipWarnings && !resolved.DryRun)
    {
        Console.Write("Continue despite warnings? [y/N] ");
        var answer = Console.ReadLine();
        if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleReporter.WriteLine(ConsoleColor.Yellow, "Aborted.");
            return 1;
        }
    }

    var outcome = generator.Run(resolved);
    ConsoleReporter.PrintOutcome(outcome);

    if (!resolved.DryRun)
        UpdateRunner.NotifyIfUpdateAvailable();

    return 0;
}
