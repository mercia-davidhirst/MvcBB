-- Matches IUserRepository.Search(string query, int skip, int take) - filters
-- by username/email, then paginates entirely in SQL instead of the caller
-- loading every user in the table first.

DROP FUNCTION IF EXISTS public.fn_search_users(text, integer, integer);

CREATE FUNCTION public.fn_search_users(
    p_query TEXT,
    p_skip INTEGER,
    p_take INTEGER
)
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    -- username/email are CITEXT, which already overloads LIKE/ILIKE to be
    -- case-insensitive - still escaped so a literal %/_/\ in the query
    -- can't act as a wildcard.
    SELECT *
    FROM public.users
    WHERE username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
       OR email ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
    ORDER BY created_at DESC
    OFFSET p_skip
    LIMIT p_take;
$$;
