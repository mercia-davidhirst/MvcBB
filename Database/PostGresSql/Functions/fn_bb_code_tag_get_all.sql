-- Matches IBBCodeManagementService.GetBBCodeTags()

DROP FUNCTION IF EXISTS public.fn_bb_code_tag_get_all();

CREATE FUNCTION public.fn_bb_code_tag_get_all()
RETURNS SETOF public.bb_code_tags
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.bb_code_tags ORDER BY sort_order;
$$;
