-- Matches IBBCodeManagementService.GetSmilie(int id)

DROP FUNCTION IF EXISTS public.fn_smilie_get_by_id(integer);

CREATE FUNCTION public.fn_smilie_get_by_id(p_id INTEGER)
RETURNS SETOF public.smilies
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.smilies WHERE id = p_id;
$$;
