namespace MvcBB.App.Interfaces
{
    /// <summary>
    /// Verifies Cloudflare Turnstile CAPTCHA responses. Both the site key and
    /// secret key are read from configuration ("Turnstile:SiteKey"/"SecretKey")
    /// rather than hardcoded, so the feature can be switched on later simply by
    /// setting those values - no code change required.
    /// </summary>
    public interface ITurnstileService
    {
        /// <summary>
        /// The public site key to render in the widget, or null if not configured.
        /// </summary>
        string? SiteKey { get; }

        /// <summary>
        /// True once both a site key and secret key are configured. While false,
        /// the widget is hidden and verification is skipped entirely so
        /// registration keeps working before the keys are set up.
        /// </summary>
        bool IsEnabled { get; }

        Task<bool> ValidateAsync(string? token, string? remoteIp);
    }
}
