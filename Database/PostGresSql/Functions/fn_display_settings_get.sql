-- Matches IDisplaySettingsRepository.Get()

DROP FUNCTION IF EXISTS public.fn_display_settings_get();

CREATE FUNCTION public.fn_display_settings_get()
RETURNS public.display_settings
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.display_settings WHERE id = 1;
$$;
