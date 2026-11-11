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
            (':)', 'Smile', '/images/smilies/smile.gif', TRUE, 1),
            (':-)', 'Smile', '/images/smilies/smile.gif', TRUE, 2),
            (':(', 'Sad', '/images/smilies/sad.gif', TRUE, 3),
            (':-(', 'Sad', '/images/smilies/sad.gif', TRUE, 4),
            (':D', 'Grin', '/images/smilies/grin.gif', TRUE, 5),
            (':-D', 'Grin', '/images/smilies/grin.gif', TRUE, 6),
            (';)', 'Wink', '/images/smilies/wink.gif', TRUE, 7),
            (';-)', 'Wink', '/images/smilies/wink.gif', TRUE, 8),
            (':P', 'Tongue', '/images/smilies/tongue.gif', TRUE, 9),
            (':-P', 'Tongue', '/images/smilies/tongue.gif', TRUE, 10),
            (':O', 'Surprised', '/images/smilies/surprised.gif', TRUE, 11),
            (':-O', 'Surprised', '/images/smilies/surprised.gif', TRUE, 12);
    END IF;
END;
$$;
