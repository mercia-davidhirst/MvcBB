-- Matches IUserRepository.GetMembersPage's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a
-- window function, same reasoning as fn_thread_get_page_count.

-- The first DROP removes the pre-email-search signature (see
-- fn_user_get_members_page).
DROP FUNCTION IF EXISTS public.fn_user_get_members_page_count(smallint, text);
DROP FUNCTION IF EXISTS public.fn_user_get_members_page_count(smallint, text, boolean);

CREATE FUNCTION public.fn_user_get_members_page_count(p_role SMALLINT, p_search TEXT, p_search_email BOOLEAN)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)
    FROM public.users AS u
    WHERE (p_role IS NULL OR u.role = p_role)
      AND (p_search IS NULL OR p_search = ''
           OR u.username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_search, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
           OR (p_search_email
               AND u.email ILIKE '%' || REPLACE(REPLACE(REPLACE(p_search, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'));
$$;
