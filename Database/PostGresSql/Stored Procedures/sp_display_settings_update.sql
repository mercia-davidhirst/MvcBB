-- Matches IDisplaySettingsRepository.Update(DisplaySettings settings)

DROP PROCEDURE IF EXISTS public.sp_display_settings_update(integer, varchar, varchar, varchar, varchar, varchar, boolean);
DROP PROCEDURE IF EXISTS public.sp_display_settings_update(integer, varchar, varchar, varchar, varchar, varchar, boolean, varchar, varchar);
DROP PROCEDURE IF EXISTS public.sp_display_settings_update(integer, varchar, varchar, varchar, varchar, varchar, boolean, varchar, varchar, varchar);
DROP PROCEDURE IF EXISTS public.sp_display_settings_update(integer, varchar, varchar, varchar, varchar, varchar, boolean, varchar, varchar, varchar, integer, integer);

CREATE PROCEDURE public.sp_display_settings_update(
    p_threads_per_page INTEGER,
    p_site_title VARCHAR(100),
    p_welcome_message VARCHAR(500),
    p_site_name VARCHAR(50),
    p_logo_path VARCHAR(500),
    p_footer_disclaimer VARCHAR(500),
    p_registration_enabled BOOLEAN,
    p_date_format VARCHAR(20),
    p_time_format VARCHAR(20),
    p_time_zone_id VARCHAR(100) DEFAULT '',
    p_max_emails_per_hour INTEGER DEFAULT 0,
    p_max_emails_per_day INTEGER DEFAULT 0
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.display_settings
    SET threads_per_page = p_threads_per_page,
        site_title = p_site_title,
        welcome_message = p_welcome_message,
        site_name = p_site_name,
        logo_path = p_logo_path,
        footer_disclaimer = p_footer_disclaimer,
        registration_enabled = p_registration_enabled,
        date_format = p_date_format,
        time_format = p_time_format,
        time_zone_id = COALESCE(p_time_zone_id, ''),
        max_emails_per_hour = COALESCE(p_max_emails_per_hour, 0),
        max_emails_per_day = COALESCE(p_max_emails_per_day, 0)
    WHERE id = 1;
END;
$$;
