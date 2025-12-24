using BizBot.Admin.Data;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;

namespace BizBot.Admin.Helpers
{
    public class SmtpEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, 
            string email, string confirmationLink)
        {
            var smtp = _config.GetSection("Smtp");

            var message = new MailMessage
            {
                From = new MailAddress(smtp["From"]!),
                Subject = "Confirm your BizBot Admin account",
                Body = $"""
                    <h2>Confirm your email</h2>
                    <p>Click the link below to activate your admin account:</p>
                    <p><a href="{confirmationLink}">Confirm Email</a></p>
                """,
                IsBodyHtml = true
            };

            message.To.Add(email);

            using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
            {
                Credentials = new NetworkCredential(
                    smtp["Username"],
                    smtp["Password"]
                ),
                EnableSsl = Convert.ToBoolean(smtp["UseSSL"])
            };

            await client.SendMailAsync(message);
        }

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            throw new NotImplementedException();
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            throw new NotImplementedException();
        }
    }
}
