-- Matches IBBCodeManagementService.GetBBCodeTag(int id)

DROP FUNCTION IF EXISTS public.fn_bb_code_tag_get_by_id(integer);

CREATE FUNCTION public.fn_bb_code_tag_get_by_id(p_id INTEGER)
RETURNS SETOF public.bb_code_tags
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.bb_code_tags WHERE id = p_id;
$$;
