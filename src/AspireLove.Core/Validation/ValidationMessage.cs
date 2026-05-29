namespace AspireLove.Core.Validation;

public enum ValidationSeverity
{
    Warning,
    Error,
}

public sealed record ValidationMessage(ValidationSeverity Severity, string Text)
{
    public static ValidationMessage Error(string text) => new(ValidationSeverity.Error, text);
    public static ValidationMessage Warning(string text) => new(ValidationSeverity.Warning, text);
}
