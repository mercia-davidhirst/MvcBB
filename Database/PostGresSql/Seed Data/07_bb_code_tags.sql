/*
    Seeds public.bb_code_tags with the default tag set currently hardcoded in
    MvcBB.App.Services.BBCodeService's constructor. Only runs if the table is
    still empty, so re-running this script won't resurrect rows an admin has
    since edited or deleted via Admin/BBCode.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.bb_code_tags) THEN
        INSERT INTO public.bb_code_tags (name, pattern, replacement, description, example, is_active, sort_order)
        VALUES
            ('Bold', '\[b\](.*?)\[/b\]', '<strong>$1</strong>', 'Makes text bold', '[b]bold text[/b]', TRUE, 1),
            ('Italic', '\[i\](.*?)\[/i\]', '<em>$1</em>', 'Makes text italic', '[i]italic text[/i]', TRUE, 2),
            ('Underline', '\[u\](.*?)\[/u\]', '<u>$1</u>', 'Underlines text', '[u]underlined text[/u]', TRUE, 3),
            ('Strike', '\[s\](.*?)\[/s\]', '<del>$1</del>', 'Strikes through text', '[s]struck text[/s]', TRUE, 4),
            ('URL', '\[url\](.*?)\[/url\]', '<a href="$1" target="_blank" rel="nofollow">$1</a>', 'Creates a hyperlink', '[url]https://example.com[/url]', TRUE, 5),
            ('URL with text', '\[url=(.*?)\](.*?)\[/url\]', '<a href="$1" target="_blank" rel="nofollow">$2</a>', 'Creates a hyperlink with custom text', '[url=https://example.com]Visit Example[/url]', TRUE, 6),
            ('Image', '\[img\](.*?)\[/img\]', '<img src="$1" alt="User posted image" class="img-fluid">', 'Displays an image', '[img]https://example.com/image.jpg[/img]', TRUE, 7),
            ('Quote', '\[quote\](.*?)\[/quote\]', '<blockquote class="blockquote">$1</blockquote>', 'Creates a quote block', '[quote]quoted text[/quote]', TRUE, 8),
            ('Quote with author', '\[quote=(.*?)\](.*?)\[/quote\]', '<blockquote class="blockquote"><p class="mb-0">$2</p><footer class="blockquote-footer">$1</footer></blockquote>', 'Creates a quote block with author attribution', '[quote=Author]quoted text[/quote]', TRUE, 9),
            ('Code', '\[code\](.*?)\[/code\]', '<pre><code>$1</code></pre>', 'Displays text in a code block', '[code]code text[/code]', TRUE, 10),
            ('Color', '\[color=(.*?)\](.*?)\[/color\]', '<span style="color: $1">$2</span>', 'Colors text', '[color=red]colored text[/color]', TRUE, 11),
            ('Size', '\[size=(.*?)\](.*?)\[/size\]', '<span style="font-size: $1px">$2</span>', 'Changes text size', '[size=20]sized text[/size]', TRUE, 12),
            ('Center', '\[center\](.*?)\[/center\]', '<div class="text-center">$1</div>', 'Centers text', '[center]centered text[/center]', TRUE, 13),
            ('Right', '\[right\](.*?)\[/right\]', '<div class="text-end">$1</div>', 'Right-aligns text', '[right]right-aligned text[/right]', TRUE, 14);
    END IF;
END;
$$;
