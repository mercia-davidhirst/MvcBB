-- Matches IModRepository.SetEnabled(string id, bool enabled) - the admin
-- Mods page toggle.

DROP PROCEDURE IF EXISTS public.sp_mod_set_enabled(varchar, boolean);

CREATE PROCEDURE public.sp_mod_set_enabled(
    p_id VARCHAR(100),
    p_is_enabled BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.mods SET is_enabled = p_is_enabled WHERE id = p_id;
END;
$$;
