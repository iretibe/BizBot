using BizBot.Client.Data;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using MimeKit;

namespace BizBot.Client.Helpers
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
            var smtpFrom = _config["EmailSettings:SmtpFrom"];
            var smtpServer = _config["EmailSettings:SmtpHost"];
            int smtpPort = Convert.ToInt32(_config["EmailSettings:SmtpPort"]);
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPwd = _config["EmailSettings:Password"];
            var useSsl = bool.Parse(_config["EmailSettings:UseSSL"]!);

            var bodyHtml = $@"
                <div style='font-family:Segoe UI, Arial, sans-serif; max-width:600px; margin:auto;'>
                    <h2 style='color:#5b2cff;'>Welcome to BizBot</h2>

                    <p>
                        Thanks for signing up for <strong>BizBot</strong>.
                        Please confirm your email address to activate your account.
                    </p>

                    <div style='text-align:center; margin:30px 0;'>
                        <a href='{confirmationLink}'
                           style='background:#5b2cff;
                                  color:#ffffff;
                                  padding:14px 28px;
                                  text-decoration:none;
                                  border-radius:6px;
                                  font-weight:600;
                                  display:inline-block;'>
                            Confirm Email
                        </a>
                    </div>

                    <p style='font-size:13px; color:#666;'>
                        If you didn’t create a BizBot account, you can safely ignore this email.
                    </p>

                    <hr style='margin-top:30px;' />

                    <p style='font-size:12px; color:#999;'>
                        © {DateTime.UtcNow.Year} BizBot. All rights reserved.
                    </p>
                </div>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", smtpFrom));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "BizBot Account Creation";
            message.Body = new BodyBuilder
            {
                HtmlBody = bodyHtml
            }.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            
            await client.ConnectAsync(smtpServer, smtpPort, useSsl);
            await client.AuthenticateAsync(smtpUser, smtpPwd);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user,
            string email, string resetCode)
        {
            var smtpFrom = _config["EmailSettings:SmtpFrom"];
            var smtpServer = _config["EmailSettings:SmtpHost"];
            int smtpPort = Convert.ToInt32(_config["EmailSettings:SmtpPort"]);
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPwd = _config["EmailSettings:Password"];
            var useSsl = bool.Parse(_config["EmailSettings:UseSSL"]!);

            var bodyHtml = $@"
                <div style='font-family:Segoe UI, Arial, sans-serif; max-width:600px; margin:auto;'>
                    <h2 style='color:#5b2cff;'>BizBot Password Reset Code</h2>

                    <p>
                        Use the verification code below to reset your password:
                    </p>

                    <div style='text-align:center; margin:30px 0;'>
                        <span style='font-size:28px;
                                     letter-spacing:6px;
                                     font-weight:700;
                                     color:#5b2cff;'>
                            {resetCode}
                        </span>
                    </div>

                    <p style='font-size:13px; color:#666;'>
                        This code expires shortly for security reasons.
                        If you didn’t request this reset, please ignore this email.
                    </p>

                    <hr style='margin-top:30px;' />

                    <p style='font-size:12px; color:#999;'>
                        © {DateTime.UtcNow.Year} BizBot. All rights reserved.
                    </p>
                </div>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("BizBot", smtpFrom));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Your BizBot password reset code";
            message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;

            await client.ConnectAsync(smtpServer, smtpPort, useSsl);
            await client.AuthenticateAsync(smtpUser, smtpPwd);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, 
            string email, string resetLink)
        {
            var smtpFrom = _config["EmailSettings:SmtpFrom"];
            var smtpServer = _config["EmailSettings:SmtpHost"];
            int smtpPort = Convert.ToInt32(_config["EmailSettings:SmtpPort"]);
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPwd = _config["EmailSettings:Password"];
            var useSsl = bool.Parse(_config["EmailSettings:UseSSL"]!);

            var bodyHtml = $@"
                <div style='font-family:Segoe UI, Arial, sans-serif; max-width:600px; margin:auto;'>
                    <h2 style='color:#5b2cff;'>Reset your BizBot password</h2>

                    <p>
                        We received a request to reset your BizBot account password.
                        Click the button below to choose a new password.
                    </p>

                    <div style='text-align:center; margin:30px 0;'>
                        <a href='{resetLink}'
                           style='background:#5b2cff;
                                  color:#ffffff;
                                  padding:14px 28px;
                                  text-decoration:none;
                                  border-radius:6px;
                                  font-weight:600;
                                  display:inline-block;'>
                            Reset Password
                        </a>
                    </div>

                    <p style='font-size:13px; color:#666;'>
                        This link will expire for security reasons.
                        If you didn’t request a password reset, you can safely ignore this email.
                    </p>

                    <hr style='margin-top:30px;' />

                    <p style='font-size:12px; color:#999;'>
                        © {DateTime.UtcNow.Year} BizBot. All rights reserved.
                    </p>
                </div>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("BizBot", smtpFrom));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Reset your BizBot password";
            message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;

            await client.ConnectAsync(smtpServer, smtpPort, useSsl);
            await client.AuthenticateAsync(smtpUser, smtpPwd);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
