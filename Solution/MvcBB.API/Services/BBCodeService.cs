using System.Text.RegularExpressions;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Services
{
    public class BBCodeService : IBBCodeService
    {
        private readonly Dictionary<string, (string pattern, string replacement)> _tags;

        public BBCodeService()
        {
            _tags = new Dictionary<string, (string, string)>
            {
                { "b", (@"\[b\](.*?)\[/b\]", "<strong>$1</strong>") },
                { "i", (@"\[i\](.*?)\[/i\]", "<em>$1</em>") },
                { "u", (@"\[u\](.*?)\[/u\]", "<u>$1</u>") },
                { "quote", (@"\[quote\](.*?)\[/quote\]", "<blockquote>$1</blockquote>") },
                { "quote=author", (@"\[quote=(.*?)\](.*?)\[/quote\]", "<blockquote><cite>$1 wrote:</cite>$2</blockquote>") },
                { "url", (@"\[url\](.*?)\[/url\]", "<a href=\"$1\">$1</a>") },
                { "url=target", (@"\[url=(.*?)\](.*?)\[/url\]", "<a href=\"$1\">$2</a>") },
                { "img", (@"\[img\](.*?)\[/img\]", "<img src=\"$1\" alt=\"\" />") },
                { "code", (@"\[code\](.*?)\[/code\]", "<pre><code>$1</code></pre>") },
                { "size", (@"\[size=([1-7])\](.*?)\[/size\]", "<span style=\"font-size:$1em;\">$2</span>") },
                { "color", (@"\[color=([#a-zA-Z0-9]+)\](.*?)\[/color\]", "<span style=\"color:$1;\">$2</span>") }
            };
        }

        public string ParseBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var result = input;
            foreach (var tag in _tags)
            {
                result = Regex.Replace(result, tag.Value.pattern, tag.Value.replacement, 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            // Convert newlines to <br> tags
            result = result.Replace("\n", "<br />");

            return result;
        }

        public string StripBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var result = input;
            foreach (var tag in _tags)
            {
                result = Regex.Replace(result, tag.Value.pattern, "$1", 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return result;
        }

        public bool ValidateBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Check for matching opening and closing tags
            var tagStack = new Stack<string>();
            var tagPattern = @"\[(/)?([a-z]+)(?:=[^\]]*)?]";
            var matches = Regex.Matches(input, tagPattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var isClosing = match.Groups[1].Success;
                var tagName = match.Groups[2].Value.ToLower();

                if (!_tags.ContainsKey(tagName) && !_tags.ContainsKey($"{tagName}=author"))
                    continue;

                if (!isClosing)
                {
                    tagStack.Push(tagName);
                }
                else if (tagStack.Count == 0 || tagStack.Pop() != tagName)
                {
                    return false;
                }
            }

            return tagStack.Count == 0;
        }
    }
} 