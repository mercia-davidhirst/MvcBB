-- Matches IModRepository.Upsert(...) - discovery/metadata only. Never touches
-- is_enabled (admin-controlled, see sp_mod_set_enabled) or installed_version
-- (set only after a successful IApiMod.InstallAsync, see sp_mod_set_installed_version).

DROP PROCEDURE IF EXISTS public.sp_mod_upsert(varchar, varchar, varchar, varchar, varchar, varchar);

CREATE PROCEDURE public.sp_mod_upsert(
    p_id VARCHAR(100),
    p_name VARCHAR(200),
    p_version VARCHAR(50),
    p_min_core_version VARCHAR(50),
    p_max_core_version VARCHAR(50),
    p_status VARCHAR(20)
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM public.mods WHERE id = p_id) THEN
        UPDATE public.mods
        SET name = p_name,
            version = p_version,
            min_core_version = p_min_core_version,
            max_core_version = p_max_core_version,
            status = p_status
        WHERE id = p_id;
    ELSE
        INSERT INTO public.mods (id, name, version, min_core_version, max_core_version, installed_version, is_enabled, status, discovered_at)
        VALUES (p_id, p_name, p_version, p_min_core_version, p_max_core_version, NULL, FALSE, p_status, (NOW() AT TIME ZONE 'utc'));
    END IF;
END;
$$;
