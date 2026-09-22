/*
    Seeds public.smilies with the default smilie set currently hardcoded in
    MvcBB.App.Services.BBCodeService's constructor. Only runs if the table is
    still empty, so re-running this script won't resurrect rows an admin has
    since edited or deleted via Admin/Smilies.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.smilies) THEN
        INSERT INTO public.smilies (code, description, image_path, is_active, sort_order)
        VALUES
            (':)', 'Smile', '/images/smilies/smile.png', TRUE, 1),
            (':-)', 'Smile', '/images/smilies/smile.png', TRUE, 2),
            (':(', 'Sad', '/images/smilies/sad.png', TRUE, 3),
            (':-(', 'Sad', '/images/smilies/sad.png', TRUE, 4),
            (':D', 'Grin', '/images/smilies/grin.png', TRUE, 5),
            (':-D', 'Grin', '/images/smilies/grin.png', TRUE, 6),
            (';)', 'Wink', '/images/smilies/wink.png', TRUE, 7),
            (';-)', 'Wink', '/images/smilies/wink.png', TRUE, 8),
            (':P', 'Tongue', '/images/smilies/tongue.png', TRUE, 9),
            (':-P', 'Tongue', '/images/smilies/tongue.png', TRUE, 10),
            (':O', 'Surprised', '/images/smilies/surprised.png', TRUE, 11),
            (':-O', 'Surprised', '/images/smilies/surprised.png', TRUE, 12);
    END IF;
END;
$$;
