using MailKit.Net.Smtp;
using MimeKit;

namespace BizBot.WebApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string email, string subject,
            string plan, string billingCycle)
        {
            var smtpFrom = _config["EmailSettings:SmtpFrom"];
            var smtpServer = _config["EmailSettings:SmtpHost"];
            int smtpPort = Convert.ToInt32(_config["EmailSettings:SmtpPort"]);
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPwd = _config["EmailSettings:Password"];
            var useSsl = bool.Parse(_config["EmailSettings:UseSSL"]!);

            var bodyHtml = $@"
                <h2>Welcome to BizBot 🎉</h2>
                <p>Hello there,</p>

                <p>Your <strong>{plan.ToUpper()}</strong> plan 
                ({billingCycle}) has been successfully activated.</p>

                <p>You can now start using BizBot Chat Integration.</p>

                <p>
                    <a href='https://bizbot.space/dashboard'>
                        Go to Dashboard
                    </a>
                </p>

                <br/>
                <p>— BizBot Team</p>
            ";

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
    }
}
