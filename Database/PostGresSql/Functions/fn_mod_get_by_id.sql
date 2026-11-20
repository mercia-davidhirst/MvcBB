-- Matches IModRepository.GetById(string id)

DROP FUNCTION IF EXISTS public.fn_mod_get_by_id(varchar);

CREATE FUNCTION public.fn_mod_get_by_id(p_id VARCHAR(100))
RETURNS SETOF public.mods
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.mods WHERE id = p_id;
$$;
