namespace MvcBB.API.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body);

        /// <summary>
        /// Sends with optional From/Reply-To overrides (admin emails to members).
        /// The default just drops the overrides, so any other IEmailSender
        /// (e.g. one a mod registers) keeps working unchanged.
        /// </summary>
        Task SendAsync(EmailMessage message) => SendAsync(message.To, message.Subject, message.Body);
    }
}
