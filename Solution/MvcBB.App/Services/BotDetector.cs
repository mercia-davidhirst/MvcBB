using System.Text.RegularExpressions;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Tells search engine crawlers, link previewers and scripts apart from
    /// people by their User-Agent header, for the home page "Who's online"
    /// card. It only labels visitors for counting; nothing is blocked.
    /// </summary>
    public static class BotDetector
    {
        // Well-known crawlers, checked first so they get a friendly name.
        // Each pair is (text to find in the user agent, name to show).
        private static readonly (string Token, string Name)[] KnownBots =
        {
            ("Googlebot", "Google"),
            ("Google-InspectionTool", "Google"),
            ("AdsBot-Google", "Google"),
            ("Mediapartners-Google", "Google"),
            ("bingbot", "Bing"),
            ("BingPreview", "Bing"),
            ("DuckDuckBot", "DuckDuckGo"),
            ("YandexBot", "Yandex"),
            ("Baiduspider", "Baidu"),
            ("Applebot", "Apple"),
            ("Slurp", "Yahoo"),
            ("facebookexternalhit", "Facebook"),
            ("meta-externalagent", "Facebook"),
            ("Twitterbot", "X (Twitter)"),
            ("LinkedInBot", "LinkedIn"),
            ("Discordbot", "Discord"),
            ("Slackbot", "Slack"),
            ("WhatsApp", "WhatsApp"),
            ("TelegramBot", "Telegram"),
            ("AhrefsBot", "Ahrefs"),
            ("SemrushBot", "Semrush"),
            ("MJ12bot", "Majestic"),
            ("DotBot", "Moz"),
            ("PetalBot", "Petal"),
            ("GPTBot", "OpenAI"),
            ("ChatGPT-User", "OpenAI"),
            ("ClaudeBot", "Anthropic"),
            ("Claude-User", "Anthropic"),
            ("CCBot", "Common Crawl"),
            ("Bytespider", "ByteDance"),
            ("PerplexityBot", "Perplexity"),
            ("Amazonbot", "Amazon"),
            ("archive.org_bot", "Internet Archive"),
            ("ia_archiver", "Internet Archive"),
        };

        // Anything else that calls itself a bot, crawler or spider, plus
        // common scripting tools and headless browsers.
        private static readonly Regex GenericBot = new(
            @"bot\b|bot/|crawl|spider|scrape|slurp|fetcher|preview|monitor|checker|headless|phantomjs|" +
            @"curl/|wget/|python-requests|python-urllib|aiohttp|httpclient|okhttp|go-http-client|java/|libwww|scrapy|node-fetch|axios/",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100));

        // Pulls a name like "FooBot" or "BarCrawler/2.1" out of the user agent.
        private static readonly Regex BotName = new(
            @"[A-Za-z0-9][A-Za-z0-9._-]*(?:bot|crawler|spider)[A-Za-z0-9._-]*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100));

        private const int MaxNameLength = 40;

        /// <summary>
        /// Returns true with a short display name (e.g. "Google") when the
        /// user agent looks like a bot. A missing user agent counts as a bot,
        /// since every real browser sends one.
        /// </summary>
        public static bool IsBot(string? userAgent, out string name)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                name = "Unknown";
                return true;
            }

            foreach (var (token, botName) in KnownBots)
            {
                if (userAgent.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    name = botName;
                    return true;
                }
            }

            try
            {
                if (GenericBot.IsMatch(userAgent))
                {
                    var match = BotName.Match(userAgent);
                    name = match.Success ? Trim(match.Value) : "Other";
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A pathological user agent: no person's browser sends one.
                name = "Other";
                return true;
            }

            name = string.Empty;
            return false;
        }

        private static string Trim(string value)
        {
            // Drop a version suffix ("FooBot/2.1" -> "FooBot") and cap the length.
            var slash = value.IndexOf('/');
            if (slash > 0)
            {
                value = value[..slash];
            }
            return value.Length > MaxNameLength ? value[..MaxNameLength] : value;
        }
    }
}
