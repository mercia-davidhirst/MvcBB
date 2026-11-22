/*
    Function: public.fn_report_content_exists
    Purpose:  reports.content_id is a polymorphic reference whose meaning
              depends on reports.type (0 = Thread, 1 = Post, 2 = Message,
              3 = User, 4 = SiteIssue), so
              it cannot be enforced with a normal foreign key. This function
              checks the row actually exists in the table implied by
              p_type, and is used by sp_report_insert.
*/

DROP FUNCTION IF EXISTS public.fn_report_content_exists(smallint, integer);

CREATE FUNCTION public.fn_report_content_exists(p_type SMALLINT, p_content_id INTEGER)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_exists BOOLEAN := FALSE;
BEGIN
    IF p_type = 0 THEN -- Thread
        SELECT TRUE INTO v_exists FROM public.threads WHERE id = p_content_id;
    ELSIF p_type = 1 THEN -- Post
        SELECT TRUE INTO v_exists FROM public.posts WHERE id = p_content_id;
    ELSIF p_type = 2 THEN -- Message
        SELECT TRUE INTO v_exists FROM public.messages WHERE id = p_content_id;
    ELSIF p_type = 3 THEN -- User
        SELECT TRUE INTO v_exists FROM public.users WHERE id = p_content_id;
    ELSIF p_type = 4 THEN -- SiteIssue: about the site, not any content, so content_id must be 0
        v_exists := (p_content_id = 0);
    END IF;

    RETURN COALESCE(v_exists, FALSE);
END;
$$;
