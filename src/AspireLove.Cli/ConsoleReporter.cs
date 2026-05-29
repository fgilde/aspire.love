using AspireLove.Core.Generation;
using AspireLove.Core.Validation;

namespace AspireLove.Cli;

/// <summary>Renders validation messages and generation results to the console.</summary>
internal static class ConsoleReporter
{
    public static void PrintValidation(ValidationResult validation)
    {
        foreach (var message in validation.Messages)
        {
            var (color, label) = message.Severity == ValidationSeverity.Error
                ? (ConsoleColor.Red, "error")
                : (ConsoleColor.Yellow, "warning");
            WriteLine(color, $"  {label}: {message.Text}");
        }
    }

    public static void PrintOutcome(GenerationOutcome outcome)
    {
        if (outcome.FilesWritten)
            WriteLine(ConsoleColor.Green, $"Generated {outcome.Files.Count} files into {outcome.OutputRoot}");
        else
            WriteLine(ConsoleColor.Cyan, $"Dry run — {outcome.Files.Count} files would be generated into {outcome.OutputRoot}:");

        foreach (var file in outcome.Files)
            Console.WriteLine($"  {(outcome.FilesWritten ? "+" : "·")} {file.RelativePath}");

        switch (outcome.PackageJson)
        {
            case PackageJsonUpdateOutcome.Added:
                WriteLine(ConsoleColor.Green, $"  Added the '{PackageJsonScriptUpdater.ScriptName}' script to package.json.");
                break;
            case PackageJsonUpdateOutcome.AlreadyPresent:
                Console.WriteLine($"  The '{PackageJsonScriptUpdater.ScriptName}' script already exists in package.json.");
                break;
            case PackageJsonUpdateOutcome.NotFound:
                WriteLine(ConsoleColor.Yellow, "  No package.json found — remember to add an 'aspire' npm script manually.");
                break;
            case PackageJsonUpdateOutcome.Invalid:
                WriteLine(ConsoleColor.Yellow, "  Could not parse package.json — the 'aspire' script was not added.");
                break;
        }
    }

    public static void WriteLine(ConsoleColor color, string text)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = previous;
    }
}
