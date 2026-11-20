-- Matches IModRepository.GetAll()

DROP FUNCTION IF EXISTS public.fn_mod_get_all();

CREATE FUNCTION public.fn_mod_get_all()
RETURNS SETOF public.mods
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.mods ORDER BY name;
$$;
