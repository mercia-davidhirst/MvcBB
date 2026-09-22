using MvcBB.Shared.Models.BBCode;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Report;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    /// <summary>
    /// Single in-memory store shared by all InMemory repositories.
    /// </summary>
    public class InMemoryStore
    {
        public List<User> Users { get; } = new();
        public List<Board> Boards { get; } = new();
        public List<ForumThread> Threads { get; } = new();
        public List<Post> Posts { get; } = new();
        public List<Message> Messages { get; } = new();
        public List<Report> Reports { get; } = new();
        public List<BBCodeTagModel> BBCodeTags { get; } = new();
        public List<SmilieModel> Smilies { get; } = new();

        public InMemoryStore()
        {
            SeedBBCodeTags();
            SeedSmilies();
        }

        // Matches Database/SQL/Seed Data/07_BBCodeTags.sql and
        // Database/PostGresSql/Seed Data/07_bb_code_tags.sql.
        private void SeedBBCodeTags()
        {
            var nextId = 1;
            void Add(string name, string pattern, string replacement, string description, string example, int sortOrder) =>
                BBCodeTags.Add(new BBCodeTagModel
                {
                    Id = nextId++,
                    Name = name,
                    Pattern = pattern,
                    Replacement = replacement,
                    Description = description,
                    Example = example,
                    IsActive = true,
                    SortOrder = sortOrder
                });

            Add("Bold", @"\[b\](.*?)\[/b\]", "<strong>$1</strong>", "Makes text bold", "[b]bold text[/b]", 1);
            Add("Italic", @"\[i\](.*?)\[/i\]", "<em>$1</em>", "Makes text italic", "[i]italic text[/i]", 2);
            Add("Underline", @"\[u\](.*?)\[/u\]", "<u>$1</u>", "Underlines text", "[u]underlined text[/u]", 3);
            Add("Strike", @"\[s\](.*?)\[/s\]", "<del>$1</del>", "Strikes through text", "[s]struck text[/s]", 4);
            Add("URL", @"\[url\](.*?)\[/url\]", "<a href=\"$1\" target=\"_blank\" rel=\"nofollow\">$1</a>", "Creates a hyperlink", "[url]https://example.com[/url]", 5);
            Add("URL with text", @"\[url=(.*?)\](.*?)\[/url\]", "<a href=\"$1\" target=\"_blank\" rel=\"nofollow\">$2</a>", "Creates a hyperlink with custom text", "[url=https://example.com]Visit Example[/url]", 6);
            Add("Image", @"\[img\](.*?)\[/img\]", "<img src=\"$1\" alt=\"User posted image\" class=\"img-fluid\">", "Displays an image", "[img]https://example.com/image.jpg[/img]", 7);
            Add("Quote", @"\[quote\](.*?)\[/quote\]", "<blockquote class=\"blockquote\">$1</blockquote>", "Creates a quote block", "[quote]quoted text[/quote]", 8);
            Add("Quote with author", @"\[quote=(.*?)\](.*?)\[/quote\]", "<blockquote class=\"blockquote\"><p class=\"mb-0\">$2</p><footer class=\"blockquote-footer\">$1</footer></blockquote>", "Creates a quote block with author attribution", "[quote=Author]quoted text[/quote]", 9);
            Add("Code", @"\[code\](.*?)\[/code\]", "<pre><code>$1</code></pre>", "Displays text in a code block", "[code]code text[/code]", 10);
            Add("Color", @"\[color=(.*?)\](.*?)\[/color\]", "<span style=\"color: $1\">$2</span>", "Colors text", "[color=red]colored text[/color]", 11);
            Add("Size", @"\[size=(.*?)\](.*?)\[/size\]", "<span style=\"font-size: $1px\">$2</span>", "Changes text size", "[size=20]sized text[/size]", 12);
            Add("Center", @"\[center\](.*?)\[/center\]", "<div class=\"text-center\">$1</div>", "Centers text", "[center]centered text[/center]", 13);
            Add("Right", @"\[right\](.*?)\[/right\]", "<div class=\"text-end\">$1</div>", "Right-aligns text", "[right]right-aligned text[/right]", 14);
        }

        // Matches Database/SQL/Seed Data/08_Smilies.sql and
        // Database/PostGresSql/Seed Data/08_smilies.sql.
        private void SeedSmilies()
        {
            var nextId = 1;
            void Add(string code, string description, string imagePath, int sortOrder) =>
                Smilies.Add(new SmilieModel
                {
                    Id = nextId++,
                    Code = code,
                    Description = description,
                    ImagePath = imagePath,
                    IsActive = true,
                    SortOrder = sortOrder
                });

            Add(":)", "Smile", "/images/smilies/smile.png", 1);
            Add(":-)", "Smile", "/images/smilies/smile.png", 2);
            Add(":(", "Sad", "/images/smilies/sad.png", 3);
            Add(":-(", "Sad", "/images/smilies/sad.png", 4);
            Add(":D", "Grin", "/images/smilies/grin.png", 5);
            Add(":-D", "Grin", "/images/smilies/grin.png", 6);
            Add(";)", "Wink", "/images/smilies/wink.png", 7);
            Add(";-)", "Wink", "/images/smilies/wink.png", 8);
            Add(":P", "Tongue", "/images/smilies/tongue.png", 9);
            Add(":-P", "Tongue", "/images/smilies/tongue.png", 10);
            Add(":O", "Surprised", "/images/smilies/surprised.png", 11);
            Add(":-O", "Surprised", "/images/smilies/surprised.png", 12);
        }
    }
}
