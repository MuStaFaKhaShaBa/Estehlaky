
using System.Net;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Abstractions.Services;
using Microsoft.Extensions.Options;
using Resend;

namespace Estehlaky.Infrastructure.Services;

/// <summary>
/// Sends transactional emails through the Resend API.
/// </summary>
public sealed class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;

    public ResendEmailService(IOptions<ResendOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _resend = ResendClient.Create(_options.ApiKey);
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(
        string toEmail,
        string otp,
        string language,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(otp);

        ct.ThrowIfCancellationRequested();

        var isArabic = IsArabic(language);
        var safeOtp = WebUtility.HtmlEncode(otp);

        var subject = isArabic
            ? "رمز التحقق الخاص بك"
            : "Your Verification Code";

        var htmlBody = isArabic
            ? BuildArabicOtpEmail(safeOtp)
            : BuildEnglishOtpEmail(safeOtp);

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlBody
        };

        await _resend.EmailSendAsync(message);
    }

    /// <inheritdoc />
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string language,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        ct.ThrowIfCancellationRequested();

        var isArabic = IsArabic(language);
        var safeName = WebUtility.HtmlEncode(fullName.Trim());

        var subject = isArabic
            ? "مرحباً بك في Estehlaky"
            : "Welcome to Estehlaky";

        var htmlBody = isArabic
            ? BuildArabicWelcomeEmail(safeName)
            : BuildEnglishWelcomeEmail(safeName);

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlBody
        };

        await _resend.EmailSendAsync(message);
    }

    private static bool IsArabic(string? language) =>
        language?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true;

    // =========================================================
    // OTP EMAIL TEMPLATES
    // =========================================================

    private static string BuildEnglishOtpEmail(string otp)
    {
        var content = $$"""
            <h1 style="margin:0 0 15px;font-size:25px;color:#0f172a;text-align:center;">
                Your Verification Code
            </h1>

            <p style="margin:0 0 30px;font-size:15px;line-height:1.8;color:#64748b;text-align:center;">
                Use the verification code below to verify your identity.
                If you did not request this code, you can safely ignore this email.
            </p>

            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                <tr>
                    <td align="center" style="background:#eff6ff;border:1px dashed #93c5fd;border-radius:14px;padding:30px 15px;">

                        <p style="margin:0 0 18px;font-size:12px;font-weight:bold;color:#64748b;text-transform:uppercase;letter-spacing:2px;">
                            Your verification code
                        </p>

                        <div dir="ltr" style="font-family:Arial,Helvetica,sans-serif;font-size:38px;font-weight:bold;color:#2563eb;letter-spacing:10px;padding:10px 0;user-select:all;-webkit-user-select:all;">
                            {{otp}}
                        </div>

                        <p style="margin:15px 0 0;font-size:12px;color:#2563eb;font-weight:bold;">
                            Select and copy your code
                        </p>

                    </td>
                </tr>
            </table>

            <p style="margin:15px 0 0;font-size:12px;color:#94a3b8;text-align:center;line-height:1.7;">
                You can select the code above and copy it to paste it into the verification field.
            </p>

            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:25px;">
                <tr>
                    <td style="background:#fff7ed;border-radius:10px;padding:16px;">
                        <p style="margin:0;font-size:13px;line-height:1.8;color:#9a3412;text-align:center;">
                            &#9200; This verification code expires in <strong>5 minutes</strong>.
                            Please do not share it with anyone.
                        </p>
                    </td>
                </tr>
            </table>

            <div style="margin-top:20px;padding:18px;background:#f8fafc;border-radius:10px;">
                <p style="margin:0;font-size:13px;line-height:1.8;color:#64748b;text-align:center;">
                    <strong style="color:#334155;">Didn't request this code?</strong>
                    <br>
                    If you didn't request a verification code, simply ignore this email.
                    Your account remains secure.
                </p>
            </div>
            """;

        return BuildEmailLayout(
            "en",
            "Account Security",
            content,
            "Keeping your account safe and secure.");
    }

    private static string BuildArabicOtpEmail(string otp)
    {
        var content = $$"""
            <h1 style="margin:0 0 15px;font-size:25px;color:#0f172a;text-align:center;">
                رمز التحقق الخاص بك
            </h1>

            <p style="margin:0 0 30px;font-size:15px;line-height:2;color:#64748b;text-align:center;">
                استخدم رمز التحقق التالي لتأكيد هويتك.
                إذا لم تطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.
            </p>

            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                <tr>
                    <td align="center" style="background:#eff6ff;border:1px dashed #93c5fd;border-radius:14px;padding:30px 15px;">

                        <p style="margin:0 0 18px;font-size:13px;font-weight:bold;color:#64748b;">
                            رمز التحقق الخاص بك
                        </p>

                        <div dir="ltr" style="font-family:Arial,Helvetica,sans-serif;font-size:38px;font-weight:bold;color:#2563eb;letter-spacing:10px;padding:10px 0;user-select:all;-webkit-user-select:all;">
                            {{otp}}
                        </div>

                        <p style="margin:15px 0 0;font-size:12px;color:#2563eb;font-weight:bold;">
                            حدد الرمز وانسخه
                        </p>

                    </td>
                </tr>
            </table>

            <p style="margin:15px 0 0;font-size:12px;color:#94a3b8;text-align:center;line-height:2;">
                يمكنك تحديد الرمز أعلاه ونسخه ثم لصقه في خانة التحقق.
            </p>

            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:25px;">
                <tr>
                    <td style="background:#fff7ed;border-radius:10px;padding:16px;">
                        <p style="margin:0;font-size:13px;line-height:2;color:#9a3412;text-align:center;">
                            &#9200; تنتهي صلاحية رمز التحقق خلال <strong>5 دقائق</strong>.
                            يرجى عدم مشاركة الرمز مع أي شخص.
                        </p>
                    </td>
                </tr>
            </table>

            <div style="margin-top:20px;padding:18px;background:#f8fafc;border-radius:10px;">
                <p style="margin:0;font-size:13px;line-height:2;color:#64748b;text-align:center;">
                    <strong style="color:#334155;">لم تطلب هذا الرمز؟</strong>
                    <br>
                    إذا لم تطلب رمز التحقق، يمكنك تجاهل هذه الرسالة بأمان.
                    حسابك لا يزال آمناً.
                </p>
            </div>
            """;

        return BuildEmailLayout(
            "ar",
            "أمان حسابك",
            content,
            "نحافظ على أمان حسابك وخصوصيتك.");
    }

    // =========================================================
    // WELCOME EMAIL TEMPLATES
    // =========================================================

    private static string BuildEnglishWelcomeEmail(string fullName)
    {
        var content = $$"""
            <h1 style="margin:0 0 15px;font-size:25px;color:#0f172a;text-align:center;">
                Welcome, {{fullName}}!
            </h1>

            <p style="margin:0 0 30px;font-size:15px;line-height:1.8;color:#64748b;text-align:center;">
                We're delighted to have you with us.
                Your account has been successfully created, and you're ready to get started.
            </p>

            <div style="margin-top:20px;padding:22px;background:#eff6ff;border-radius:14px;text-align:center;">
                <p style="margin:0 0 10px;font-size:17px;font-weight:bold;color:#2563eb;">
                    We're glad you're here!
                </p>

                <p style="margin:0;font-size:14px;line-height:1.8;color:#64748b;">
                    Thank you for joining Estehlaky.
                    We look forward to being part of your journey.
                </p>
            </div>
            """;

        return BuildEmailLayout(
            "en",
            "Welcome to Estehlaky",
            content,
            "We're happy to have you as part of our community.");
    }

    private static string BuildArabicWelcomeEmail(string fullName)
    {
        var content = $$"""
            <h1 style="margin:0 0 15px;font-size:25px;color:#0f172a;text-align:center;">
                أهلاً بك، {{fullName}}!
            </h1>

            <p style="margin:0 0 30px;font-size:15px;line-height:2;color:#64748b;text-align:center;">
                يسعدنا انضمامك إلينا.
                تم إنشاء حسابك بنجاح، وأصبحت الآن جاهزاً للبدء.
            </p>

            <div style="margin-top:20px;padding:22px;background:#eff6ff;border-radius:14px;text-align:center;">
                <p style="margin:0 0 10px;font-size:17px;font-weight:bold;color:#2563eb;">
                    سعداء بوجودك معنا!
                </p>

                <p style="margin:0;font-size:14px;line-height:2;color:#64748b;">
                    شكراً لانضمامك إلى Estehlaky.
                    نتطلع لأن نكون جزءاً من رحلتك.
                </p>
            </div>
            """;

        return BuildEmailLayout(
            "ar",
            "مرحباً بك في Estehlaky",
            content,
            "يسعدنا انضمامك إلى مجتمعنا.");
    }

    // =========================================================
    // SHARED EMAIL LAYOUT
    // =========================================================

    private static string BuildEmailLayout(
        string language,
        string headerSubtitle,
        string content,
        string footerDescription)
    {
        var isArabic = IsArabic(language);
        var direction = isArabic ? "rtl" : "ltr";
        var font = isArabic
            ? "Tahoma,Arial,sans-serif"
            : "Arial,Helvetica,sans-serif";

        return $$"""
            <!DOCTYPE html>
            <html lang="{{language}}" dir="{{direction}}">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta name="color-scheme" content="light">
                <title>Estehlaky</title>
            </head>

            <body dir="{{direction}}" style="margin:0;padding:0;background:#f1f5f9;font-family:{{font}};color:#1e293b;">

                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
                       style="background:#f1f5f9;padding:40px 15px;">
                    <tr>
                        <td align="center">

                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
                                   style="max-width:520px;background:#ffffff;border-radius:18px;overflow:hidden;box-shadow:0 5px 25px rgba(15,23,42,0.06);">

                                <!-- Header -->
                                <tr>
                                    <td align="center" style="background:#2563eb;padding:35px 20px;">

                                        <div style="font-size:30px;font-weight:bold;color:#ffffff;letter-spacing:1px;">
                                            Estehlaky
                                        </div>

                                        <p style="margin:8px 0 0;font-size:13px;color:#dbeafe;">
                                            {{headerSubtitle}}
                                        </p>

                                    </td>
                                </tr>

                                <!-- Content -->
                                <tr>
                                    <td dir="{{direction}}" style="padding:40px 30px 30px;">
                                        {{content}}
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td align="center" style="background:#f8fafc;border-top:1px solid #e2e8f0;padding:25px 20px;">

                                        <p style="margin:0 0 8px;font-size:13px;font-weight:bold;color:#475569;">
                                            Estehlaky
                                        </p>

                                        <p style="margin:0 0 8px;font-size:11px;color:#94a3b8;">
                                            {{footerDescription}}
                                        </p>

                                        <p style="margin:0;font-size:11px;color:#94a3b8;">
                                            © {{DateTime.UtcNow.Year}} Estehlaky. All rights reserved.
                                        </p>

                                        <p style="margin:10px 0 0;font-size:10px;color:#cbd5e1;">
                                            This is an automated message. Please do not reply.
                                        </p>

                                    </td>
                                </tr>

                            </table>

                        </td>
                    </tr>
                </table>

            </body>
            </html>
            """;
    }
}
