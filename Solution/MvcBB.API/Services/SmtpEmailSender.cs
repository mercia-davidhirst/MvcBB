using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Sends real email via SMTP, configured entirely from the Smtp:* keys in
    /// appsettings.json (Host, Port, UseSsl, Username, Password, FromEmail,
    /// FromName) - deliberately not a secrets-store/environment-variable setup,
    /// since MvcBB is a white-label project that may end up on shared hosting
    /// where nothing beyond a config file is available.
    ///
    /// Registered in place of LoggingEmailSender only once Smtp:Host is
    /// actually set - see Program.cs - so a fresh install with no SMTP
    /// configured keeps today's log-only behavior rather than failing.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Smtp:Host"]
                ?? throw new InvalidOperationException("Smtp:Host is not configured");
            var port = _configuration.GetValue("Smtp:Port", 587);
            var useSsl = _configuration.GetValue("Smtp:UseSsl", true);
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"]
                ?? throw new InvalidOperationException("Smtp:FromEmail is not configured");
            var fromName = _configuration["Smtp:FromName"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.IsNullOrEmpty(fromName) ? fromEmail : fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            // Port 465 is always implicit TLS (the connection is encrypted from
            // the first byte); 587/25 negotiate encryption after connecting via
            // STARTTLS, which UseSsl controls - some internal/legacy relays on
            // 25 genuinely run unencrypted.
            var secureSocketOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : useSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, secureSocketOptions);

                if (!string.IsNullOrEmpty(username))
                {
                    await client.AuthenticateAsync(username, password ?? string.Empty);
                }

                // MailKit 5.0's SmtpClient declares its own SendAsync(ISendRequest, ...)
                // instance overload, which shadows the IMailTransportExtensions.SendAsync
                // extension method that accepts a plain MimeMessage - so it has to be
                // called explicitly as a static method instead of client.SendAsync(message).
                await IMailTransportExtensions.SendAsync(client, message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
