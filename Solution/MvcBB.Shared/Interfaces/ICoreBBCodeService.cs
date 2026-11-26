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

        /// <summary>
        /// As above, and also links each @mention of a username in
        /// mentionedUsernames to that member's profile (a post's
        /// PostResponse.MentionedUsernames - the mentions the API found to be
        /// real members). Other @words stay plain text.
        /// </summary>
        string ParseBBCode(string input, IReadOnlyDictionary<string, string>? smilies, IReadOnlyCollection<string>? mentionedUsernames);

        /// <summary>
        /// BBCode removed, for plain-text summaries. A spoiler's text is
        /// replaced with "(spoiler)" so it stays hidden there too.
        /// </summary>
        string StripBBCode(string input);
        bool ValidateBBCode(string input);
    }
} 