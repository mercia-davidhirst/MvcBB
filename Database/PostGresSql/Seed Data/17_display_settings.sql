/*
    Seeds public.display_settings with its one singleton row, matching
    MvcBB.Shared.Models.Settings.DisplaySettings' own property defaults
    (the same values the old static-field-backed DisplaySettingsService
    used to fall back to). Only runs if the table is still empty, so
    re-running this script won't overwrite values an admin has since
    changed via Admin/Settings.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.display_settings) THEN
        INSERT INTO public.display_settings (id, threads_per_page, site_title, welcome_message, site_name, logo_path, footer_disclaimer, registration_enabled, date_format, time_format)
        VALUES
            (1, 20, 'Welcome to MvcBB', 'A modern bulletin board system for engaging discussions and community building.', 'MvcBB', NULL, NULL, TRUE, 'dd/MM/yyyy', 'HH:mm');
    END IF;
END;
$$;
