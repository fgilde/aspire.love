namespace AspireLove.Core.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationMessage> _messages = [];

    public IReadOnlyList<ValidationMessage> Messages => _messages;

    public IEnumerable<ValidationMessage> Errors =>
        _messages.Where(m => m.Severity == ValidationSeverity.Error);

    public IEnumerable<ValidationMessage> Warnings =>
        _messages.Where(m => m.Severity == ValidationSeverity.Warning);

    public bool IsValid => _messages.All(m => m.Severity != ValidationSeverity.Error);

    public bool HasWarnings => _messages.Any(m => m.Severity == ValidationSeverity.Warning);

    public void AddError(string text) => _messages.Add(ValidationMessage.Error(text));

    public void AddWarning(string text) => _messages.Add(ValidationMessage.Warning(text));
}
