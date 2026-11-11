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
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Multiple passes let innermost tags resolve first, so nested tags
            // (e.g. [b][i]text[/i][/b]) still match once the inner tag's content
            // no longer breaks the non-greedy capture across a "[".
            var output = input;
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

                    return tag switch
                    {
                        "b" => $"<strong>{content}</strong>",
                        "i" => $"<em>{content}</em>",
                        "u" => $"<u>{content}</u>",
                        "s" => $"<s>{content}</s>",
                        "quote" => $"<blockquote>{content}</blockquote>",
                        "code" => $"<pre><code>{content}</code></pre>",
                        "center" => $"<div style=\"text-align: center;\">{content}</div>",
                        "right" => $"<div style=\"text-align: right;\">{content}</div>",
                        "url" => $"<a href=\"{WebUtility.HtmlEncode(value ?? content)}\" target=\"_blank\" rel=\"noopener noreferrer\">{content}</a>",
                        "img" => $"<img src=\"{WebUtility.HtmlEncode(content)}\" alt=\"\" />",
                        "color" when value != null && SafeColorPattern.IsMatch(value) => $"<span style=\"color: {value};\">{content}</span>",
                        "size" when value != null && SafeSizePattern.IsMatch(value) => $"<span style=\"font-size: {value}px;\">{content}</span>",
                        _ => match.Value // Keep original if tag not recognized (or value failed validation)
                    };
                });
            } while (output != previous && ++pass < 10);

            if (smilies != null && smilies.Count > 0)
            {
                output = ReplaceSmilies(output, smilies);
            }

            return output;
        }

        // Runs after BBCode tag parsing, since smilie codes (":)", ";-D", ...)
        // contain no "[" or "]" and so can't interfere with tag matching.
        // Longest-code-first so e.g. ":-)" matches before the shorter ":)"
        // would otherwise consume part of it.
        private static string ReplaceSmilies(string html, IReadOnlyDictionary<string, string> smilies)
        {
            foreach (var pair in smilies.OrderByDescending(p => p.Key.Length))
            {
                html = html.Replace(pair.Key, pair.Value);
            }

            return html;
        }

        public string StripBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return BBCodeStripPattern.Replace(input, string.Empty).Trim();
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