using System.Text.RegularExpressions;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Pure computation, no repository access - callers fetch the active
    /// filters once per request and reuse this against as much text as they
    /// need (a thread's title and its first post, a reply, an edit, ...).
    /// </summary>
    public static class WordFilterApplier
    {
        public static string Apply(string input, IEnumerable<WordFilterModel> filters)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var output = input;
            foreach (var filter in filters.Where(f => f.IsActive && !string.IsNullOrWhiteSpace(f.Word)))
            {
                var pattern = $@"\b{Regex.Escape(filter.Word)}\b";
                output = Regex.Replace(output, pattern, filter.Replacement, RegexOptions.IgnoreCase);
            }

            return output;
        }
    }
}
