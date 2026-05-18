namespace Platform.Payment.API.Infrastructure.Configurations;

public class SandboxPaymentOptions
{
    public bool Enabled { get; set; }
    public string PublicBaseUrl { get; set; } = string.Empty;
}
