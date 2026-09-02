namespace ArchivioLessicale.API.Models.Options;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }

    public int LifeSpanInMonths { get; set; }
    public DateTime CutoffDate => DateTime.UtcNow.AddMonths(-LifeSpanInMonths);
}
