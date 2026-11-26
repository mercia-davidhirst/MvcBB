-- Matches IModerationSettingsRepository.Get()

DROP FUNCTION IF EXISTS public.fn_moderation_settings_get();

CREATE FUNCTION public.fn_moderation_settings_get()
RETURNS SETOF public.moderation_settings
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.moderation_settings WHERE id = 1;
$$;
