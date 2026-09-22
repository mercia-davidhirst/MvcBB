-- Matches IBBCodeManagementService.GetSmilies()

DROP FUNCTION IF EXISTS public.fn_smilie_get_all();

CREATE FUNCTION public.fn_smilie_get_all()
RETURNS SETOF public.smilies
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.smilies ORDER BY sort_order;
$$;
