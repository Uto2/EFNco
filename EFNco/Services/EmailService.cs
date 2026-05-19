using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace EFNco.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task SendOvertimeAlertAsync(string to, string name, string plate, int overtimeMinutes, double maxHours);
        Task SendPasswordResetAsync(string to, string name, string resetLink);
        Task SendEmailConfirmationAsync(string to, string name, string confirmLink);
        Task SendPermitApprovedAsync(string to, string name, string plate, DateTime validUntil);
        Task SendPermitRejectedAsync(string to, string name, string plate, string? reason);
        Task SendViolationIssuedAsync(string to, string name, string plate, string violationType, decimal fine);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ── Core send method ──────────────────────────────────
        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host     = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var port     = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var username = _config["Email:Username"] ?? "";
            var password = _config["Email:Password"] ?? "";
            var from     = _config["Email:From"] ?? username;

            using var client = new SmtpClient(host, port)
            {
                Credentials    = new NetworkCredential(username, password),
                EnableSsl      = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mail = new MailMessage
            {
                From       = new MailAddress(from, "EFNco Parking System"),
                Subject    = subject,
                Body       = WrapInTemplate(subject, htmlBody),
                IsBodyHtml = true
            };

            mail.To.Add(to);
            await client.SendMailAsync(mail);
        }

        // ── 7.6 Overtime Alert ────────────────────────────────
        public async Task SendOvertimeAlertAsync(string to, string name, string plate, int overtimeMinutes, double maxHours)
        {
            var subject = $"⚠ Overtime Parking Alert — {plate}";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>Your vehicle <strong>{plate}</strong> has exceeded the maximum allowed parking duration.</p>
                <table style='background:#1a1a2e;border-radius:8px;padding:16px;width:100%;'>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Max Allowed</td>
                        <td style='color:#e2e8f0;font-weight:600;'>{maxHours} hours</td></tr>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Overtime By</td>
                        <td style='color:#fca5a5;font-weight:600;'>{overtimeMinutes} minutes</td></tr>
                </table>
                <p style='margin-top:16px;'>Please return to your vehicle immediately to avoid a violation fine.</p>
                <p style='color:#64748b;font-size:13px;'>If you believe this is an error, please contact the parking office.</p>";

            await SendAsync(to, subject, body);
        }

        // ── 7.8 Password Reset ────────────────────────────────
        public async Task SendPasswordResetAsync(string to, string name, string resetLink)
        {
            var subject = "Reset Your EFNco Password";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>We received a request to reset your password. Click the button below to set a new one.</p>
                <p style='text-align:center;margin:28px 0;'>
                    <a href='{resetLink}'
                       style='background:#3b82f6;color:white;padding:12px 28px;border-radius:8px;
                              text-decoration:none;font-weight:600;font-size:15px;display:inline-block;'>
                        Reset Password
                    </a>
                </p>
                <p style='color:#64748b;font-size:13px;'>
                    This link expires in <strong>24 hours</strong>. If you didn't request a password reset,
                    you can safely ignore this email.
                </p>";

            await SendAsync(to, subject, body);
        }

        // ── 7.8 Email Confirmation ────────────────────────────
        public async Task SendEmailConfirmationAsync(string to, string name, string confirmLink)
        {
            var subject = "Confirm Your EFNco Account";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>Welcome to the EFNco Parking System! Please confirm your email address to activate your account.</p>
                <p style='text-align:center;margin:28px 0;'>
                    <a href='{confirmLink}'
                       style='background:#10b981;color:white;padding:12px 28px;border-radius:8px;
                              text-decoration:none;font-weight:600;font-size:15px;display:inline-block;'>
                        Confirm Email
                    </a>
                </p>
                <p style='color:#64748b;font-size:13px;'>
                    If you did not create an EFNco account, please ignore this email.
                </p>";

            await SendAsync(to, subject, body);
        }

        // ── Permit Approved ───────────────────────────────────
        public async Task SendPermitApprovedAsync(string to, string name, string plate, DateTime validUntil)
        {
            var subject = $"✅ Permit Approved — {plate}";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>Great news! Your parking permit application has been <strong style='color:#6ee7b7;'>approved</strong>.</p>
                <table style='background:#1a1a2e;border-radius:8px;padding:16px;width:100%;'>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Plate Number</td>
                        <td style='color:#e2e8f0;font-weight:600;'>{plate}</td></tr>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Valid Until</td>
                        <td style='color:#e2e8f0;font-weight:600;'>{validUntil:MMMM dd, yyyy}</td></tr>
                </table>
                <p style='margin-top:16px;'>Log in to view your digital permit and QR code.</p>";

            await SendAsync(to, subject, body);
        }

        // ── Permit Rejected ───────────────────────────────────
        public async Task SendPermitRejectedAsync(string to, string name, string plate, string? reason)
        {
            var subject = $"Permit Application Update — {plate}";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>Unfortunately, your parking permit application for <strong>{plate}</strong> has been
                   <strong style='color:#fca5a5;'>rejected</strong>.</p>
                {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                <p>You may reapply after addressing the issue. Contact the parking office if you have questions.</p>";

            await SendAsync(to, subject, body);
        }

        // ── Violation Issued ──────────────────────────────────
        public async Task SendViolationIssuedAsync(string to, string name, string plate, string violationType, decimal fine)
        {
            var subject = $"⚠ Parking Violation Issued — {plate}";
            var body = $@"
                <p>Hi <strong>{name}</strong>,</p>
                <p>A parking violation has been issued for your vehicle <strong>{plate}</strong>.</p>
                <table style='background:#1a1a2e;border-radius:8px;padding:16px;width:100%;'>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Violation</td>
                        <td style='color:#e2e8f0;font-weight:600;'>{violationType}</td></tr>
                    <tr><td style='color:#94a3b8;padding:4px 0;'>Fine Amount</td>
                        <td style='color:#fca5a5;font-weight:700;font-size:18px;'>₱{fine:N2}</td></tr>
                </table>
                <p style='margin-top:16px;'>Log in to view details, pay the fine, or submit an appeal.</p>";

            await SendAsync(to, subject, body);
        }

        // ── HTML wrapper template ─────────────────────────────
        private static string WrapInTemplate(string title, string content) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#0b0f1a;font-family:Inter,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#0b0f1a;padding:40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0'
             style='background:#131929;border:1px solid #1e2d45;border-radius:16px;overflow:hidden;'>
        <tr>
          <td style='background:#0f172a;padding:24px 32px;border-bottom:1px solid #1e2d45;'>
            <span style='font-size:20px;font-weight:700;color:#3b82f6;letter-spacing:2px;'>EFNco</span>
            <span style='font-size:13px;color:#475569;margin-left:8px;'>Parking System</span>
          </td>
        </tr>
        <tr>
          <td style='padding:32px;color:#e2e8f0;font-size:15px;line-height:1.6;'>
            {content}
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;border-top:1px solid #1e2d45;text-align:center;'>
            <span style='font-size:12px;color:#334155;'>
              &copy; {DateTime.Now.Year} EFNco Parking System. Do not reply to this email.
            </span>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }
}
