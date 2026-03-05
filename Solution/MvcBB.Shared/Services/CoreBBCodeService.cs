using System.Text.RegularExpressions;
using MvcBB.Shared.Interfaces;

namespace MvcBB.Shared.Services
{
    public class CoreBBCodeService : ICoreBBCodeService
    {
        private static readonly Regex BBCodePattern = new(@"\[([^\]]+)\]([^\[]*)\[/\1\]", RegexOptions.Compiled);
        private static readonly Regex BBCodeStripPattern = new(@"\[.*?\]", RegexOptions.Compiled);

        public string ParseBBCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Basic BBCode parsing - this can be enhanced based on specific BBCode tags
            var output = input;
            output = BBCodePattern.Replace(output, match =>
            {
                var tag = match.Groups[1].Value.ToLower();
                var content = match.Groups[2].Value;

                return tag switch
                {
                    "b" => $"<strong>{content}</strong>",
                    "i" => $"<em>{content}</em>",
                    "u" => $"<u>{content}</u>",
                    "quote" => $"<blockquote>{content}</blockquote>",
                    "code" => $"<pre><code>{content}</code></pre>",
                    _ => match.Value // Keep original if tag not recognized
                };
            });

            return output;
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