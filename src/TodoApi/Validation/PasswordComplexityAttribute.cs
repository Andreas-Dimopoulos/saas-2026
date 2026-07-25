using System.ComponentModel.DataAnnotations;

namespace TodoApi.Validation;

public sealed class PasswordComplexityAttribute : ValidationAttribute
{
    public int MinimumLength { get; init; } = 8;

    public override bool IsValid(object? value) =>
        value is string password
        && password.Length >= MinimumLength
        && password.Any(char.IsDigit)
        && password.Any(char.IsLetter);

    public override string FormatErrorMessage(string name) =>
        $"{name} must be at least {MinimumLength} characters and contain both a letter and a digit.";
}
