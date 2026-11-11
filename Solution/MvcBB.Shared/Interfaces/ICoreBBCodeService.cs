namespace MvcBB.Shared.Interfaces
{
    public interface ICoreBBCodeService
    {
        /// <summary>
        /// Parses BBCode tags, then (if a smilie dictionary is supplied)
        /// replaces each smilie code with its image markup. The dictionary
        /// is keyed by code (e.g. ":)") with the value being the smilie's
        /// ready-to-render HTML - callers already have exactly this shape
        /// (IMvcBBCodeService.GetAvailableSmiliesAsync) since it's also what
        /// drives the smilie toolbar/picker.
        /// </summary>
        string ParseBBCode(string input, IReadOnlyDictionary<string, string>? smilies = null);
        string StripBBCode(string input);
        bool ValidateBBCode(string input);
    }
} 