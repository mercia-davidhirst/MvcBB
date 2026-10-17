namespace MvcBB.API.Services
{
    /// <summary>
    /// Default IEmailSender: there's no SMTP server configured anywhere in
    /// this project, so for now this just logs what would have been sent
    /// (including the reset link itself, so the password reset flow is fully
    /// usable in dev without real email delivery). Swap in a real
    /// SmtpEmailSender against the same interface when SMTP credentials are
    /// available.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("Email to {ToEmail}: {Subject}\n{Body}", toEmail, subject, body);
            return Task.CompletedTask;
        }
    }
}
