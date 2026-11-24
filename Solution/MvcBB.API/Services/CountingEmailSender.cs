using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Wraps the real IEmailSender (SMTP or logging) and, for each email sent
    /// successfully, counts it in EmailSendCounter (so admin emails can keep
    /// under the host's limits) and writes its time and kind to the send log
    /// (the Sending activity chart). It never delays anything itself:
    /// password resets and other site emails go straight through, and a
    /// problem writing the log is only logged.
    /// </summary>
    public class CountingEmailSender : IEmailSender
    {
        private readonly IEmailSender _inner;
        private readonly EmailSendCounter _counter;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CountingEmailSender> _logger;

        public CountingEmailSender(
            IEmailSender inner,
            EmailSendCounter counter,
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            ILogger<CountingEmailSender> logger)
        {
            _inner = inner;
            _counter = counter;
            _scopeFactory = scopeFactory;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            await _inner.SendAsync(toEmail, subject, body);
            Record(EmailKind.Other);
        }

        public async Task SendAsync(EmailMessage message)
        {
            await _inner.SendAsync(message);
            Record(message.Kind);
        }

        private void Record(EmailKind kind)
        {
            _counter.Record();
            try
            {
                // The repository is scoped for the database providers (one
                // connection per scope), while this sender is a singleton.
                using var scope = _scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<IEmailSendLogRepository>()
                    .Add(_timeProvider.GetUtcNow().UtcDateTime, kind);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't add a sent {Kind} email to the send log", kind);
            }
        }
    }
}
