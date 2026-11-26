using System.Net;
using System.Text.RegularExpressions;
using MvcBB.Shared.Interfaces;
using System.Linq;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Implements IBBCodeService (not just ICoreBBCodeService) so a single
    /// instance can satisfy either interface without a wrapper - MvcBB.App
    /// injects it directly as IBBCodeService in views for parse/strip/validate.
    /// </summary>
    public class CoreBBCodeService : IBBCodeService
    {
        // Group 1 is the bare tag name only, so a valued opening tag like
        // [color=red] still pairs with a plain [/color] closing tag - the old
        // pattern captured "color=red" into the backreference, which could
        // never match a same-named closing tag and silently left every valued
        // tag (color, size) unparsed.
        private static readonly Regex BBCodePattern = new(
            @"\[(\w+)(?:=([^\]]*))?\](.*?)\[/\1\]",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex BBCodeStripPattern = new(@"\[.*?\]", RegexOptions.Compiled);
        private static readonly Regex SafeColorPattern = new(@"^[a-zA-Z]+$|^#[0-9a-fA-F]{3}([0-9a-fA-F]{3})?$", RegexOptions.Compiled);
        private static readonly Regex SafeSizePattern = new(@"^\d{1,3}$", RegexOptions.Compiled);

        public string ParseBBCode(string input, IReadOnlyDictionary<string, string>? smilies = null)
            => ParseBBCode(input, smilies, mentionedUsernames: null);

        /// <summary>
        /// Same as ParseBBCode(input, smilies), and also turns every @mention
        /// of a username in <paramref name="mentionedUsernames"/> into a link
        /// to that member's profile (see MentionParser). A mention of anyone
        /// else stays plain text, so a typo or an email-style "@word" never
        /// links to a profile that doesn't exist.
        /// </summary>
        public string ParseBBCode(string input, IReadOnlyDictionary<string, string>? smilies, IReadOnlyCollection<string>? mentionedUsernames)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Encode first, so no user-supplied markup ever reaches the output as
            // HTML. HtmlEncode leaves "[", "]", "=" and "/" untouched, so the BBCode
            // pattern below still matches the encoded text, and every piece of
            // HTML in the result is built by this method alone. Anything that
            // fails validation stays as (already encoded) plain text.
            // Normalise line endings up front so the line-break pass below only
            // has to deal with "\n" (browsers submit textareas with "\r\n").
            var output = WebUtility.HtmlEncode(input.Replace("\r\n", "\n").Replace('\r', '\n'));

            // Multiple passes let innermost tags resolve first, so nested tags
            // (e.g. [b][i]text[/i][/b]) still match once the inner tag's content
            // no longer breaks the non-greedy capture across a "[".
            string previous;
            var pass = 0;
            do
            {
                previous = output;
                output = BBCodePattern.Replace(output, match =>
                {
                    var tag = match.Groups[1].Value.ToLowerInvariant();
                    var value = match.Groups[2].Success ? match.Groups[2].Value : null;
                    var content = match.Groups[3].Value;

                    switch (tag)
                    {
                        case "b": return $"<strong>{content}</strong>";
                        case "i": return $"<em>{content}</em>";
                        case "u": return $"<u>{content}</u>";
                        case "s": return $"<s>{content}</s>";
                        case "quote": return $"<blockquote>{content}</blockquote>";
                        case "code": return $"<pre><code>{content}</code></pre>";
                        case "center": return $"<div style=\"text-align: center;\">{content}</div>";
                        case "right": return $"<div style=\"text-align: right;\">{content}</div>";
                        case "spoiler":
                        {
                            // [spoiler]text[/spoiler] or [spoiler=Title]text[/spoiler].
                            // Both the title and the content are already
                            // HTML-encoded (see the top of this method).
                            var title = string.IsNullOrWhiteSpace(value) ? "Spoiler" : value.Trim();
                            return $"<details class=\"bbcode-spoiler\"><summary>{title}</summary><div class=\"bbcode-spoiler-body\">{content}</div></details>";
                        }
                        case "url":
                        {
                            var href = TryGetSafeUrl(value ?? content, allowMailto: true);
                            return href == null
                                ? match.Value
                                : $"<a href=\"{WebUtility.HtmlEncode(href)}\" target=\"_blank\" rel=\"noopener noreferrer nofollow\">{content}</a>";
                        }
                        case "img":
                        {
                            var src = TryGetSafeUrl(content, allowMailto: false);
                            return src == null
                                ? match.Value
                                : $"<img src=\"{WebUtility.HtmlEncode(src)}\" alt=\"\" />";
                        }
                        case "color" when value != null && SafeColorPattern.IsMatch(value):
                            return $"<span style=\"color: {value};\">{content}</span>";
                        case "size" when value != null && SafeSizePattern.IsMatch(value):
                            return $"<span style=\"font-size: {value}px;\">{content}</span>";
                        default:
                            return match.Value; // Keep original if tag not recognized (or value failed validation)
                    }
                });
            } while (output != previous && ++pass < 10);

            output = ConvertLineBreaks(output);

            if (mentionedUsernames != null && mentionedUsernames.Count > 0 && output.IndexOf('@') >= 0)
            {
                output = LinkMentions(output, mentionedUsernames);
            }

            if (smilies != null && smilies.Count > 0)
            {
                output = ReplaceSmilies(output, smilies);
            }

            return output;
        }

        // A single newline hugging a block-level tag we generate (quote, code,
        // center/right) is just how people lay the tags out on their own lines;
        // turning it into a <br /> as well would add a stray blank line.
        private static readonly Regex BlockTagNewlinePattern = new(
            @"\n?(</?(?:blockquote|pre|div|details|summary)\b[^>]*>)\n?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PreBlockPattern = new(
            @"(<pre\b[^>]*>.*?</pre>)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Turns the user's line breaks into &lt;br /&gt; so paragraphs survive
        /// HTML rendering (where raw newlines collapse into a single space).
        /// [code] blocks are left alone: they keep their newlines verbatim and
        /// rely on white-space: pre-wrap instead. Each newline is replaced rather
        /// than kept alongside the &lt;br /&gt;, so a container that itself uses
        /// white-space: pre-wrap/pre-line doesn't render every break twice.
        /// </summary>
        private static string ConvertLineBreaks(string html)
        {
            if (html.IndexOf('\n') < 0)
                return html;

            html = BlockTagNewlinePattern.Replace(html, "$1");

            var parts = PreBlockPattern.Split(html);
            for (var i = 0; i < parts.Length; i++)
            {
                if (!parts[i].StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
                    parts[i] = parts[i].Replace("\n", "<br />");
            }

            return string.Concat(parts);
        }

        /// <summary>
        /// Returns the decoded URL if it is safe to put in an href/src: an absolute
        /// http(s) URL (or mailto: for links), or a site-relative path starting with
        /// a single "/". Anything else - javascript:, data:, vbscript:, other
        /// schemes, or text that still contains markup - returns null.
        /// </summary>
        private static string? TryGetSafeUrl(string encodedValue, bool allowMailto)
        {
            var url = WebUtility.HtmlDecode(encodedValue).Trim();
            if (url.Length == 0 || url.IndexOfAny(new[] { '<', '>', '"', '\'', '`' }) >= 0 || url.Any(char.IsControl))
                return null;

            if (url.StartsWith('/'))
            {
                // "//host" and "/\host" are treated by browsers as another site;
                // only allow plain site-relative paths here.
                return url.Length > 1 && (url[1] == '/' || url[1] == '\\') ? null : url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ||
                 (allowMailto && uri.Scheme == Uri.UriSchemeMailto)))
            {
                return url;
            }

            return null;
        }

        public const string MentionProfileUrlPrefix = "/Account/Profile?username=";

        private static readonly Regex SkipOpenTagPattern = new(@"^<(a|pre|code)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SkipCloseTagPattern = new(@"^</(a|pre|code)\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Runs after BBCode tag parsing, on the text between tags only, and
        // never inside a link or a code block. Works on the decoded text (so
        // a quoted name like @&quot;Some Name&quot; is seen as @"Some Name")
        // and encodes everything it writes back.
        private static string LinkMentions(string html, IReadOnlyCollection<string> mentionedUsernames)
        {
            var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in mentionedUsernames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    known[name] = name;
                }
            }
            if (known.Count == 0)
            {
                return html;
            }

            var parts = HtmlTagPattern.Split(html);
            var skipDepth = 0;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length == 0)
                {
                    continue;
                }

                if (part[0] == '<')
                {
                    if (SkipOpenTagPattern.IsMatch(part))
                    {
                        skipDepth++;
                    }
                    else if (SkipCloseTagPattern.IsMatch(part))
                    {
                        skipDepth = Math.Max(0, skipDepth - 1);
                    }
                    continue;
                }

                if (skipDepth > 0 || part.IndexOf('@') < 0)
                {
                    continue;
                }

                var text = WebUtility.HtmlDecode(part);
                var result = new System.Text.StringBuilder();
                var last = 0;
                foreach (Match m in MentionParser.MentionPattern.Matches(text))
                {
                    var name = MentionParser.NameOf(m);
                    if (!known.TryGetValue(name, out var username))
                    {
                        continue;
                    }

                    // A bare @word may have picked up a trailing "." or "-"
                    // (end of a sentence) that isn't part of the name.
                    var length = m.Groups["w"].Success ? 1 + name.Length : m.Length;
                    result.Append(WebUtility.HtmlEncode(text.Substring(last, m.Index - last)));
                    result.Append("<a class=\"mention\" href=\"")
                          .Append(WebUtility.HtmlEncode(MentionProfileUrlPrefix + Uri.EscapeDataString(username)))
                          .Append("\">@")
                          .Append(WebUtility.HtmlEncode(username))
                          .Append("</a>");
                    last = m.Index + length;
                }

                if (last > 0)
                {
                    result.Append(WebUtility.HtmlEncode(text.Substring(last)));
                    parts[i] = result.ToString();
                }
            }

            return string.Concat(parts);
        }

        // Runs after BBCode tag parsing. Smilies are only replaced in text between
        // HTML tags (never inside a generated tag's attributes), and on the decoded
        // text, so a code like ";)" can't land inside an entity such as "&quot;)"
        // and break it. Longest-code-first so e.g. ":-)" matches before the
        // shorter ":)" would otherwise consume part of it.
        private static readonly Regex HtmlTagPattern = new(@"(<[^>]*>)", RegexOptions.Compiled);

        private static string ReplaceSmilies(string html, IReadOnlyDictionary<string, string> smilies)
        {
            var codes = smilies.Keys.Where(k => !string.IsNullOrEmpty(k)).OrderByDescending(k => k.Length).ToList();
            if (codes.Count == 0)
                return html;

            var smiliePattern = new Regex(string.Join("|", codes.Select(Regex.Escape)));
            var parts = HtmlTagPattern.Split(html);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || parts[i][0] == '<')
                    continue;

                var text = WebUtility.HtmlDecode(parts[i]);
                var result = new System.Text.StringBuilder();
                var last = 0;
                foreach (Match m in smiliePattern.Matches(text))
                {
                    result.Append(WebUtility.HtmlEncode(text.Substring(last, m.Index - last)));
                    result.Append(smilies[m.Value]);
                    last = m.Index + m.Length;
                }
                result.Append(WebUtility.HtmlEncode(text.Substring(last)));
                parts[i] = result.ToString();
            }

            return string.Concat(parts);
        }

        private static readonly Regex SpoilerBlockPattern = new(
            @"\[spoiler(?:=[^\]]*)?\](?:(?!\[spoiler(?:=[^\]]*)?\]).)*?\[/spoiler\]",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public string StripBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // A spoiler's text stays hidden in plain-text summaries (recent
            // posts, notifications), innermost spoiler first so nested ones
            // can't leak their outer text.
            var text = input;
            if (text.IndexOf("[spoiler", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string previous;
                var pass = 0;
                do
                {
                    previous = text;
                    text = SpoilerBlockPattern.Replace(text, "\u0001");
                } while (text != previous && ++pass < 20);
                text = text.Replace("\u0001", "(spoiler)");
            }

            return BBCodeStripPattern.Replace(text, string.Empty).Trim();
        }

        public bool ValidateBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Check for matching opening and closing tags
            var stack = new Stack<string>();
            var tagPattern = new Regex(@"\[(/)?([^\]]+)\]");
            var matches = tagPattern.Matches(input);

            foreach (Match match in matches)
            {
                var isClosing = match.Groups[1].Success;
                var tag = match.Groups[2].Value.Split('=')[0].ToLower();

                if (!isClosing)
                {
                    stack.Push(tag);
                }
                else
                {
                    if (stack.Count == 0 || stack.Pop() != tag)
                        return false;
                }
            }

            return stack.Count == 0;
        }
    }
} 