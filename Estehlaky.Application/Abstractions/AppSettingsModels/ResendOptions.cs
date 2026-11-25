namespace Estehlaky.Application.Abstractions.AppSettingsModels;

/// <summary>
/// Defines configuration settings for the Resend email provider.
/// </summary>
public sealed class ResendOptions
{
    public const string SectionName = "EmailServices:Resend";

    public string ApiKey { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;
}