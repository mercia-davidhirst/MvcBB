-- Matches IModRepository.SetInstalledVersion(string id, string version) -
-- called once IApiMod.InstallAsync has completed successfully for that version.

DROP PROCEDURE IF EXISTS public.sp_mod_set_installed_version(varchar, varchar);

CREATE PROCEDURE public.sp_mod_set_installed_version(
    p_id VARCHAR(100),
    p_version VARCHAR(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.mods SET installed_version = p_version WHERE id = p_id;
END;
$$;
