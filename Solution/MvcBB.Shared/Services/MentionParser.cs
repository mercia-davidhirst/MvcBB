using System.Text.RegularExpressions;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Finds @mentions in post text. A mention is "@Name" for a username made
    /// of letters, digits, "_", "." and "-", or "@"Some Name"" (in double
    /// quotes) for any other username, such as one with a space in it. The
    /// editor's autocomplete inserts whichever form the name needs.
    ///
    /// Mentions inside [quote] and [code] blocks don't count when deciding
    /// who to notify (quoting a post shouldn't notify everyone it mentioned
    /// all over again), but they're still linked when the post is shown.
    /// </summary>
    public static class MentionParser
    {
        public const int MaxMentionsPerPost = 20;

        // Not preceded by a word character or another "@", so an email address
        // (bob@example.com) is never read as a mention of "example.com".
        public static readonly Regex MentionPattern = new(
            @"(?<![\w@])@(?:""(?<q>[^""\r\n]{3,50})""|(?<w>[\p{L}\p{N}_][\p{L}\p{N}_.\-]{1,49}))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex InnermostQuoteOrCode = new(
            @"\[(quote|code)(?:=[^\]]*)?\](?:(?!\[(?:quote|code)(?:=[^\]]*)?\]).)*?\[/\1\]",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>The username a match refers to (quotes removed, trailing "." or "-" dropped).</summary>
        public static string NameOf(Match match)
        {
            if (match.Groups["q"].Success)
            {
                return match.Groups["q"].Value.Trim();
            }

            return match.Groups["w"].Value.TrimEnd('.', '-');
        }

        /// <summary>
        /// The distinct usernames mentioned in the text (case-insensitive, in
        /// the order first mentioned, at most MaxMentionsPerPost). With
        /// includeQuotedText false, [quote] and [code] blocks are skipped.
        /// </summary>
        public static IReadOnlyList<string> Extract(string? content, bool includeQuotedText = false)
        {
            if (string.IsNullOrEmpty(content) || content.IndexOf('@') < 0)
            {
                return Array.Empty<string>();
            }

            var text = content;
            if (!includeQuotedText)
            {
                string previous;
                var pass = 0;
                do
                {
                    previous = text;
                    text = InnermostQuoteOrCode.Replace(text, " ");
                } while (text != previous && ++pass < 20);
            }

            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in MentionPattern.Matches(text))
            {
                var name = NameOf(match);
                if (name.Length >= 3 && seen.Add(name))
                {
                    names.Add(name);
                    if (names.Count >= MaxMentionsPerPost)
                    {
                        break;
                    }
                }
            }

            return names;
        }

        /// <summary>How the editor writes a mention of this username.</summary>
        public static string Format(string username) =>
            Regex.IsMatch(username, @"^[\p{L}\p{N}_][\p{L}\p{N}_.\-]*[\p{L}\p{N}_]$")
                ? "@" + username
                : "@\"" + username + "\"";
    }
}
