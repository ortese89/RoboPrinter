namespace BackEnds.RoboPrinter.Models;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;

    public JwtOptions(string _secret, string _issuer, string _audience)
    {
        Secret = _secret;
        Issuer = _issuer;
        Audience = _audience;
    }
}
