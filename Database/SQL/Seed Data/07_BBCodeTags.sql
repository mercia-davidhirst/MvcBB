/*
    Seeds dbo.BBCodeTags with the default tag set currently hardcoded in
    MvcBB.App.Services.BBCodeService's constructor. Only runs if the table is
    still empty, so re-running this script won't resurrect rows an admin has
    since edited or deleted via Admin/BBCode.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.BBCodeTags)
BEGIN
    INSERT INTO dbo.BBCodeTags (Name, Pattern, Replacement, Description, Example, IsActive, SortOrder)
    VALUES
        (N'Bold', N'\[b\](.*?)\[/b\]', N'<strong>$1</strong>', N'Makes text bold', N'[b]bold text[/b]', 1, 1),
        (N'Italic', N'\[i\](.*?)\[/i\]', N'<em>$1</em>', N'Makes text italic', N'[i]italic text[/i]', 1, 2),
        (N'Underline', N'\[u\](.*?)\[/u\]', N'<u>$1</u>', N'Underlines text', N'[u]underlined text[/u]', 1, 3),
        (N'Strike', N'\[s\](.*?)\[/s\]', N'<del>$1</del>', N'Strikes through text', N'[s]struck text[/s]', 1, 4),
        (N'URL', N'\[url\](.*?)\[/url\]', N'<a href="$1" target="_blank" rel="nofollow">$1</a>', N'Creates a hyperlink', N'[url]https://example.com[/url]', 1, 5),
        (N'URL with text', N'\[url=(.*?)\](.*?)\[/url\]', N'<a href="$1" target="_blank" rel="nofollow">$2</a>', N'Creates a hyperlink with custom text', N'[url=https://example.com]Visit Example[/url]', 1, 6),
        (N'Image', N'\[img\](.*?)\[/img\]', N'<img src="$1" alt="User posted image" class="img-fluid">', N'Displays an image', N'[img]https://example.com/image.jpg[/img]', 1, 7),
        (N'Quote', N'\[quote\](.*?)\[/quote\]', N'<blockquote class="blockquote">$1</blockquote>', N'Creates a quote block', N'[quote]quoted text[/quote]', 1, 8),
        (N'Quote with author', N'\[quote=(.*?)\](.*?)\[/quote\]', N'<blockquote class="blockquote"><p class="mb-0">$2</p><footer class="blockquote-footer">$1</footer></blockquote>', N'Creates a quote block with author attribution', N'[quote=Author]quoted text[/quote]', 1, 9),
        (N'Code', N'\[code\](.*?)\[/code\]', N'<pre><code>$1</code></pre>', N'Displays text in a code block', N'[code]code text[/code]', 1, 10),
        (N'Color', N'\[color=(.*?)\](.*?)\[/color\]', N'<span style="color: $1">$2</span>', N'Colors text', N'[color=red]colored text[/color]', 1, 11),
        (N'Size', N'\[size=(.*?)\](.*?)\[/size\]', N'<span style="font-size: $1px">$2</span>', N'Changes text size', N'[size=20]sized text[/size]', 1, 12),
        (N'Center', N'\[center\](.*?)\[/center\]', N'<div class="text-center">$1</div>', N'Centers text', N'[center]centered text[/center]', 1, 13),
        (N'Right', N'\[right\](.*?)\[/right\]', N'<div class="text-end">$1</div>', N'Right-aligns text', N'[right]right-aligned text[/right]', 1, 14);
END;
GO
