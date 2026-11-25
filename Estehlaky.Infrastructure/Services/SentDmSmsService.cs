using Sentdm;
using Sentdm.Models.Messages;
using Estehlaky.Application.Abstractions.Services;

namespace Estehlaky.Infrastructure.Sms;

public sealed class SentDmSmsService(ISentClient sentClient) : ISmsService
{
    private const string _passwordResetTemplateId =
        "73679844-df5a-4112-98d8-12d72a4f6b78";

    public async Task SendOtpAsync(
        string toPhoneNumber,
        string otp,
        string language,
        CancellationToken ct)
    {
        var parameters = new MessageSendParams
        {
            To = [toPhoneNumber],
            Channel = ["sms"],
            Template = new()
            {
                ID = _passwordResetTemplateId,
                Parameters = new Dictionary<string, string>()
                {
                    { "otp", otp}
                }
            }
        };

        var result = await sentClient.Messages.Send(parameters, ct);

        if (result.Success != true)
        {
            throw new InvalidOperationException(
                $"Sent.dm failed to send OTP. Error: {result.Error?.Message}");
        }
    }
}