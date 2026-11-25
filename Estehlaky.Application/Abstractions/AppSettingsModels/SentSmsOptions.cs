namespace Estehlaky.Infrastructure.Sms;

public sealed class SentSmsOptions
{
    public const string SectionName = "SentSms";

    public string BaseUrl { get; set; } 
    public string ApiKey { get; set; } = string.Empty;
}