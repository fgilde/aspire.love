using AspireLove.Core.Validation;

namespace AspireLove.Core;

/// <summary>Thrown when generation is attempted with options that fail validation.</summary>
public sealed class GenerationValidationException(ValidationResult result)
    : Exception("Generation options are invalid: "
        + string.Join("; ", result.Errors.Select(e => e.Text)))
{
    public ValidationResult Result { get; } = result;
}
