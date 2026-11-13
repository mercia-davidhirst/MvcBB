-- Matches IUserRepository.Search's total count (the full filtered count,
-- not just the page returned) - a separate call rather than a window
-- function, same reasoning as fn_thread_get_page_count.

DROP FUNCTION IF EXISTS public.fn_search_users_count(text);

CREATE FUNCTION public.fn_search_users_count(p_query TEXT)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)
    FROM public.users
    WHERE username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
       OR email ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\';
$$;
