/*
    Function: public.fn_report_content_exists
    Purpose:  reports.content_id is a polymorphic reference whose meaning
              depends on reports.type (0 = Thread, 1 = Post, 2 = Message), so
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
    END IF;

    RETURN COALESCE(v_exists, FALSE);
END;
$$;
