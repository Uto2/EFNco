using System.Net;
using System.Net.Mail;

namespace EFNco.Services
{
    public interface IEmailService
    {
        Task SendViolationNoticeAsync(string toEmail, string toName, string violationType, decimal fine, string plateNumber, DateTime issuedAt);
        Task SendAppealResultAsync(string toEmail, string toName, bool approved, string? adminResponse);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        private async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user = _config["Email:Username"] ?? "";
            var pass = _config["Email:Password"] ?? "";
            var from = _config["Email:From"] ?? "noreply@efnco.com";

            using var client = new SmtpClient(smtp, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var mail = new MailMessage(from, toEmail, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }

        public async Task SendViolationNoticeAsync(string toEmail, string toName,
            string violationType, decimal fine, string plateNumber, DateTime issuedAt)
        {
            var subject = $"[EFNco] Parking Violation Notice — {violationType}";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#f8fafc;padding:32px;'>
    <div style='max-width:560px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;'>
        <div style='background:#0a0f1e;padding:24px 32px;'>
            <h1 style='color:white;font-size:20px;margin:0;'>EFNco</h1>
            <p style='color:#94a3b8;font-size:12px;margin:4px 0 0;'>Efficient Facility Network Control</p>
        </div>
        <div style='padding:28px 32px;'>
            <div style='background:#fee2e2;border:1px solid #fca5a5;border-radius:8px;padding:16px;margin-bottom:24px;'>
                <h2 style='color:#991b1b;font-size:16px;margin:0 0 4px;'>⚠️ Parking Violation Notice</h2>
                <p style='color:#7f1d1d;font-size:13px;margin:0;'>A violation has been issued against your vehicle.</p>
            </div>
            <p style='color:#1e293b;font-size:14px;'>Dear <strong>{toName}</strong>,</p>
            <p style='color:#475569;font-size:14px;line-height:1.6;'>
                A parking violation has been recorded for your vehicle. Please review the details below and settle the fine at the facility office.
            </p>
            <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
                <tr style='border-bottom:1px solid #f1f5f9;'>
                    <td style='padding:10px 0;font-size:13px;color:#64748b;width:40%;'>Violation Type</td>
                    <td style='padding:10px 0;font-size:13px;font-weight:600;color:#1e293b;'>{violationType}</td>
                </tr>
                <tr style='border-bottom:1px solid #f1f5f9;'>
                    <td style='padding:10px 0;font-size:13px;color:#64748b;'>Plate Number</td>
                    <td style='padding:10px 0;font-size:14px;font-weight:800;color:#1e293b;letter-spacing:2px;'>{plateNumber}</td>
                </tr>
                <tr style='border-bottom:1px solid #f1f5f9;'>
                    <td style='padding:10px 0;font-size:13px;color:#64748b;'>Fine Amount</td>
                    <td style='padding:10px 0;font-size:16px;font-weight:800;color:#dc2626;'>₱{fine:N2}</td>
                </tr>
                <tr>
                    <td style='padding:10px 0;font-size:13px;color:#64748b;'>Date Issued</td>
                    <td style='padding:10px 0;font-size:13px;color:#1e293b;'>{issuedAt:MMMM dd, yyyy h:mm tt}</td>
                </tr>
            </table>
            <p style='color:#475569;font-size:13px;'>
                You may appeal this violation through the EFNco portal. Log in and go to <strong>My Violations</strong> to submit an appeal.
            </p>
        </div>
        <div style='background:#f8fafc;padding:16px 32px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;'>
            This is an automated message from EFNco. Do not reply to this email.
        </div>
    </div>
</body>
</html>";

            await SendAsync(toEmail, subject, body);
        }

        public async Task SendAppealResultAsync(string toEmail, string toName,
            bool approved, string? adminResponse)
        {
            var subject = approved
                ? "[EFNco] Your Violation Appeal — Approved"
                : "[EFNco] Your Violation Appeal — Rejected";

            var statusColor  = approved ? "#065f46" : "#991b1b";
            var statusBg     = approved ? "#d1fae5" : "#fee2e2";
            var statusBorder = approved ? "#6ee7b7" : "#fca5a5";
            var statusText   = approved ? "✅ Appeal Approved" : "❌ Appeal Rejected";
            var statusMsg    = approved
                ? "Your appeal has been approved. The violation has been dismissed."
                : "Your appeal has been reviewed and rejected. The fine remains payable.";

            var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Arial,sans-serif;background:#f8fafc;padding:32px;'>
    <div style='max-width:560px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;'>
        <div style='background:#0a0f1e;padding:24px 32px;'>
            <h1 style='color:white;font-size:20px;margin:0;'>EFNco</h1>
            <p style='color:#94a3b8;font-size:12px;margin:4px 0 0;'>Efficient Facility Network Control</p>
        </div>
        <div style='padding:28px 32px;'>
            <div style='background:{statusBg};border:1px solid {statusBorder};border-radius:8px;padding:16px;margin-bottom:24px;'>
                <h2 style='color:{statusColor};font-size:16px;margin:0 0 4px;'>{statusText}</h2>
                <p style='color:{statusColor};font-size:13px;margin:0;'>{statusMsg}</p>
            </div>
            <p style='color:#1e293b;font-size:14px;'>Dear <strong>{toName}</strong>,</p>
            {(string.IsNullOrEmpty(adminResponse) ? "" : $"<p style='color:#475569;font-size:14px;'><strong>Admin Response:</strong> {adminResponse}</p>")}
            <p style='color:#475569;font-size:13px;'>Log in to EFNco to view the full details of your violation.</p>
        </div>
        <div style='background:#f8fafc;padding:16px 32px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;'>
            This is an automated message from EFNco. Do not reply to this email.
        </div>
    </div>
</body>
</html>";

            await SendAsync(toEmail, subject, body);
        }
    }
}
