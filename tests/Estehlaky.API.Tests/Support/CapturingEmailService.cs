
using System.Collections.Concurrent;
using Estehlaky.Application.Abstractions.Services;

namespace Estehlaky.API.Tests.Support;

/// <summary>
/// Represents a captured OTP email sent during testing.
/// </summary>
public sealed record SentOtpEmail(string To, string Otp, string Language);

/// <summary>
/// Represents a captured welcome email sent during testing.
/// </summary>
public sealed record SentWelcomeEmail(string To, string FullName, string Language);

/// <summary>
/// Replaces the real email sender so tests can inspect sent emails
/// without making external API calls.
/// </summary>
public sealed class CapturingEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentOtpEmail> _sentOtps = new();
    private readonly ConcurrentQueue<SentWelcomeEmail> _sentWelcomeEmails = new();

    public IReadOnlyCollection<SentOtpEmail> SentOtps => [.. _sentOtps];

    public IReadOnlyCollection<SentWelcomeEmail> SentWelcomeEmails => [.. _sentWelcomeEmails];

    public SentOtpEmail? LastOtpFor(string email) =>
        _sentOtps.LastOrDefault(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));

    public SentWelcomeEmail? LastWelcomeFor(string email) =>
        _sentWelcomeEmails.LastOrDefault(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));

    public Task SendOtpAsync(
        string toEmail,
        string otp,
        string language,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _sentOtps.Enqueue(new SentOtpEmail(toEmail, otp, language));

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string language,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _sentWelcomeEmails.Enqueue(
            new SentWelcomeEmail(toEmail, fullName, language));

        return Task.CompletedTask;
    }
}
