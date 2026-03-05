using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;
using MvcBB.Shared.Services;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Service for managing BBCode tags and smilies in the MVC application
    /// </summary>
    public class BBCodeService : IMvcBBCodeService
    {
        private readonly List<BBCodeTagModel> _bbCodeTags;
        private readonly List<SmilieModel> _smilies;
        private readonly ICoreBBCodeService _coreBBCodeService;
        private int _nextBBCodeId = 1;
        private int _nextSmilieId = 1;

        public BBCodeService(ICoreBBCodeService coreBBCodeService)
        {
            _coreBBCodeService = coreBBCodeService;
            _bbCodeTags = new List<BBCodeTagModel>();
            _smilies = new List<SmilieModel>();

            // Add default BBCode tags
            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Bold",
                Pattern = @"\[b\](.*?)\[/b\]",
                Replacement = "<strong>$1</strong>",
                Description = "Makes text bold",
                Example = "[b]bold text[/b]",
                IsActive = true,
                SortOrder = 1
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Italic",
                Pattern = @"\[i\](.*?)\[/i\]",
                Replacement = "<em>$1</em>",
                Description = "Makes text italic",
                Example = "[i]italic text[/i]",
                IsActive = true,
                SortOrder = 2
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Underline",
                Pattern = @"\[u\](.*?)\[/u\]",
                Replacement = "<u>$1</u>",
                Description = "Underlines text",
                Example = "[u]underlined text[/u]",
                IsActive = true,
                SortOrder = 3
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Strike",
                Pattern = @"\[s\](.*?)\[/s\]",
                Replacement = "<del>$1</del>",
                Description = "Strikes through text",
                Example = "[s]struck text[/s]",
                IsActive = true,
                SortOrder = 4
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "URL",
                Pattern = @"\[url\](.*?)\[/url\]",
                Replacement = "<a href=\"$1\" target=\"_blank\" rel=\"nofollow\">$1</a>",
                Description = "Creates a hyperlink",
                Example = "[url]https://example.com[/url]",
                IsActive = true,
                SortOrder = 5
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "URL with text",
                Pattern = @"\[url=(.*?)\](.*?)\[/url\]",
                Replacement = "<a href=\"$1\" target=\"_blank\" rel=\"nofollow\">$2</a>",
                Description = "Creates a hyperlink with custom text",
                Example = "[url=https://example.com]Visit Example[/url]",
                IsActive = true,
                SortOrder = 6
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Image",
                Pattern = @"\[img\](.*?)\[/img\]",
                Replacement = "<img src=\"$1\" alt=\"User posted image\" class=\"img-fluid\">",
                Description = "Displays an image",
                Example = "[img]https://example.com/image.jpg[/img]",
                IsActive = true,
                SortOrder = 7
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Quote",
                Pattern = @"\[quote\](.*?)\[/quote\]",
                Replacement = "<blockquote class=\"blockquote\">$1</blockquote>",
                Description = "Creates a quote block",
                Example = "[quote]quoted text[/quote]",
                IsActive = true,
                SortOrder = 8
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Quote with author",
                Pattern = @"\[quote=(.*?)\](.*?)\[/quote\]",
                Replacement = "<blockquote class=\"blockquote\"><p class=\"mb-0\">$2</p><footer class=\"blockquote-footer\">$1</footer></blockquote>",
                Description = "Creates a quote block with author attribution",
                Example = "[quote=Author]quoted text[/quote]",
                IsActive = true,
                SortOrder = 9
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Code",
                Pattern = @"\[code\](.*?)\[/code\]",
                Replacement = "<pre><code>$1</code></pre>",
                Description = "Displays text in a code block",
                Example = "[code]code text[/code]",
                IsActive = true,
                SortOrder = 10
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Color",
                Pattern = @"\[color=(.*?)\](.*?)\[/color\]",
                Replacement = "<span style=\"color: $1\">$2</span>",
                Description = "Colors text",
                Example = "[color=red]colored text[/color]",
                IsActive = true,
                SortOrder = 11
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Size",
                Pattern = @"\[size=(.*?)\](.*?)\[/size\]",
                Replacement = "<span style=\"font-size: $1px\">$2</span>",
                Description = "Changes text size",
                Example = "[size=20]sized text[/size]",
                IsActive = true,
                SortOrder = 12
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Center",
                Pattern = @"\[center\](.*?)\[/center\]",
                Replacement = "<div class=\"text-center\">$1</div>",
                Description = "Centers text",
                Example = "[center]centered text[/center]",
                IsActive = true,
                SortOrder = 13
            });

            AddBBCodeTag(new BBCodeTagModel
            {
                Name = "Right",
                Pattern = @"\[right\](.*?)\[/right\]",
                Replacement = "<div class=\"text-end\">$1</div>",
                Description = "Right-aligns text",
                Example = "[right]right-aligned text[/right]",
                IsActive = true,
                SortOrder = 14
            });

            // Add default smilies
            AddSmilie(new SmilieModel
            {
                Code = ":)",
                Description = "Smile",
                ImagePath = "/images/smilies/smile.png",
                IsActive = true,
                SortOrder = 1
            });

            AddSmilie(new SmilieModel
            {
                Code = ":-)",
                Description = "Smile",
                ImagePath = "/images/smilies/smile.png",
                IsActive = true,
                SortOrder = 2
            });

            AddSmilie(new SmilieModel
            {
                Code = ":(",
                Description = "Sad",
                ImagePath = "/images/smilies/sad.png",
                IsActive = true,
                SortOrder = 3
            });

            AddSmilie(new SmilieModel
            {
                Code = ":-(",
                Description = "Sad",
                ImagePath = "/images/smilies/sad.png",
                IsActive = true,
                SortOrder = 4
            });

            AddSmilie(new SmilieModel
            {
                Code = ":D",
                Description = "Grin",
                ImagePath = "/images/smilies/grin.png",
                IsActive = true,
                SortOrder = 5
            });

            AddSmilie(new SmilieModel
            {
                Code = ":-D",
                Description = "Grin",
                ImagePath = "/images/smilies/grin.png",
                IsActive = true,
                SortOrder = 6
            });

            AddSmilie(new SmilieModel
            {
                Code = ";)",
                Description = "Wink",
                ImagePath = "/images/smilies/wink.png",
                IsActive = true,
                SortOrder = 7
            });

            AddSmilie(new SmilieModel
            {
                Code = ";-)",
                Description = "Wink",
                ImagePath = "/images/smilies/wink.png",
                IsActive = true,
                SortOrder = 8
            });

            AddSmilie(new SmilieModel
            {
                Code = ":P",
                Description = "Tongue",
                ImagePath = "/images/smilies/tongue.png",
                IsActive = true,
                SortOrder = 9
            });

            AddSmilie(new SmilieModel
            {
                Code = ":-P",
                Description = "Tongue",
                ImagePath = "/images/smilies/tongue.png",
                IsActive = true,
                SortOrder = 10
            });

            AddSmilie(new SmilieModel
            {
                Code = ":O",
                Description = "Surprised",
                ImagePath = "/images/smilies/surprised.png",
                IsActive = true,
                SortOrder = 11
            });

            AddSmilie(new SmilieModel
            {
                Code = ":-O",
                Description = "Surprised",
                ImagePath = "/images/smilies/surprised.png",
                IsActive = true,
                SortOrder = 12
            });
        }

        public string ParseBBCode(string input)
        {
            return _coreBBCodeService.ParseBBCode(input);
        }

        public string StripBBCode(string input)
        {
            return _coreBBCodeService.StripBBCode(input);
        }

        public bool ValidateBBCode(string input)
        {
            return _coreBBCodeService.ValidateBBCode(input);
        }

        public IEnumerable<BBCodeTagModel> GetBBCodeTags()
        {
            return _bbCodeTags.OrderBy(t => t.SortOrder);
        }

        public BBCodeTagModel GetBBCodeTag(int id)
        {
            return _bbCodeTags.FirstOrDefault(t => t.Id == id);
        }

        public void AddBBCodeTag(BBCodeTagModel model)
        {
            model.Id = _nextBBCodeId++;
            _bbCodeTags.Add(model);
        }

        public void UpdateBBCodeTag(int id, BBCodeTagModel model)
        {
            var existing = _bbCodeTags.FirstOrDefault(t => t.Id == id);
            if (existing != null)
            {
                existing.Name = model.Name;
                existing.Pattern = model.Pattern;
                existing.Replacement = model.Replacement;
                existing.Description = model.Description;
                existing.Example = model.Example;
                existing.IsActive = model.IsActive;
                existing.SortOrder = model.SortOrder;
            }
        }

        public void DeleteBBCodeTag(int id)
        {
            _bbCodeTags.RemoveAll(t => t.Id == id);
        }

        public IEnumerable<SmilieModel> GetSmilies()
        {
            return _smilies.OrderBy(s => s.SortOrder);
        }

        public SmilieModel GetSmilie(int id)
        {
            return _smilies.FirstOrDefault(s => s.Id == id);
        }

        public void AddSmilie(SmilieModel model)
        {
            model.Id = _nextSmilieId++;
            _smilies.Add(model);
        }

        public void UpdateSmilie(int id, SmilieModel model)
        {
            var existing = _smilies.FirstOrDefault(s => s.Id == id);
            if (existing != null)
            {
                existing.Code = model.Code;
                existing.Description = model.Description;
                existing.ImagePath = model.ImagePath;
                existing.IsActive = model.IsActive;
                existing.SortOrder = model.SortOrder;
            }
        }

        public void DeleteSmilie(int id)
        {
            _smilies.RemoveAll(s => s.Id == id);
        }

        public Dictionary<string, string> GetAvailableSmilies()
        {
            return _smilies
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToDictionary(
                    s => s.Code,
                    s => $"<img src=\"{s.ImagePath}\" alt=\"{s.Description}\" class=\"smilie\" />"
                );
        }
    }
} 