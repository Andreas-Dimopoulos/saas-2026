using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
