using AspireLove.Core.Options;

namespace AspireLove.Core.Validation;

/// <summary>
/// Validates that a set of <see cref="GenerationOptions"/> can actually produce a working
/// AppHost against the target Lovable project. Shared by the CLI and the WPF UI so both
/// surface the same errors and warnings.
/// </summary>
public static class GenerationValidator
{
    public static ValidationResult Validate(GenerationOptions options)
    {
        var result = new ValidationResult();

        ValidateProjectPath(options, result);
        ValidateModeRequirements(options, result);
        ValidateMonitoring(options, result);
        ValidateExistingOutput(options, result);

        return result;
    }

    private static void ValidateProjectPath(GenerationOptions options, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(options.LovableProjectPath))
        {
            result.AddError("No Lovable project path was provided.");
            return;
        }

        if (!Directory.Exists(options.LovableProjectPath))
        {
            result.AddError($"The path '{options.LovableProjectPath}' does not exist or is not a directory.");
            return;
        }

        if (!File.Exists(Path.Combine(options.LovableProjectPath, "package.json")))
            result.AddWarning("No package.json found — this does not look like a Lovable/Vite project.");

        if (options.UsesLocalSupabase)
        {
            var supabaseDir = Path.Combine(options.LovableProjectPath, "supabase");
            if (!Directory.Exists(supabaseDir))
            {
                result.AddWarning(
                    "No 'supabase' folder found. Migrations and edge functions cannot be applied locally.");
            }
            else if (options.Mode == GenerationMode.FullLocal)
            {
                if (!Directory.Exists(Path.Combine(supabaseDir, "migrations")))
                    result.AddWarning("No 'supabase/migrations' folder found — WithMigrations will have nothing to apply.");
                if (!Directory.Exists(Path.Combine(supabaseDir, "functions")))
                    result.AddWarning("No 'supabase/functions' folder found — WithEdgeFunctions will have nothing to serve.");
            }
        }
    }

    private static void ValidateModeRequirements(GenerationOptions options, ValidationResult result)
    {
        switch (options.Mode)
        {
            case GenerationMode.SupabaseSync:
                if (options.SyncInfo is null)
                {
                    result.AddError("Supabase Sync mode requires sync credentials (ProjectRef, ServiceKey, DbPassword, ManagementToken).");
                }
                else
                {
                    RequireValue(result, options.SyncInfo.ProjectRef, "Project Ref");
                    RequireValue(result, options.SyncInfo.ServiceKey, "Service Key");
                    RequireValue(result, options.SyncInfo.DbPassword, "Database Password");
                    RequireValue(result, options.SyncInfo.ManagementToken, "Management Token");
                }
                break;

            case GenerationMode.RemoteConnect:
                if (options.RemoteInfo is null)
                {
                    result.AddError("Remote Connect mode requires a Project Ref and Service Key.");
                }
                else
                {
                    RequireValue(result, options.RemoteInfo.ProjectRef, "Project Ref");
                    RequireValue(result, options.RemoteInfo.ServiceKey, "Service Key");
                }
                break;
        }
    }

    private static void ValidateMonitoring(GenerationOptions options, ValidationResult result)
    {
        if (options.AddMonitoring && !options.UsesLocalSupabase)
            result.AddError("Monitoring is only available with a local Supabase stack (Full Local or Supabase Sync).");
    }

    private static void ValidateExistingOutput(GenerationOptions options, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(options.LovableProjectPath))
            return;

        var aspireDir = Path.Combine(options.LovableProjectPath, "aspire");
        if (Directory.Exists(aspireDir))
            result.AddWarning("An 'aspire' folder already exists and matching files will be overwritten.");
    }

    private static void RequireValue(ValidationResult result, string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            result.AddError($"{fieldName} must not be empty.");
    }
}
