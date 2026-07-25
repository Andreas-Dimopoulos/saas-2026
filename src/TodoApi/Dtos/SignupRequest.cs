using System.ComponentModel.DataAnnotations;
using TodoApi.Validation;

namespace TodoApi.Dtos;

public record SignupRequest(
    [Required, EmailAddress] string Email,
    [Required, PasswordComplexity] string Password);
