namespace TodoApi.Models;

public class RevokedToken
{
    public required string Jti { get; set; }
    public DateTime ExpiresAt { get; set; }
}
