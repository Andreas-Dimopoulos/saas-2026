namespace TodoApi.Dtos;

public record LoginResponse(string Token, DateTime ExpiresAt);
