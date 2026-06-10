namespace Auth.Infrastructure.Security;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "shop-platform";
    public string Audience { get; set; } = "shop-platform";
    public int ExpiresDays { get; set; } = 7;
}

