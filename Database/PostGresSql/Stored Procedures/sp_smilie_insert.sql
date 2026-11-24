-- Matches IBBCodeManagementService.AddSmilie(SmilieModel model)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_smilie_insert(varchar, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_smilie_insert(
    p_code VARCHAR(20),
    p_description VARCHAR(200),
    p_image_path VARCHAR(500),
    p_is_active BOOLEAN,
    p_sort_order INTEGER,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.smilies (code, description, image_path, is_active, sort_order)
    VALUES (p_code, p_description, p_image_path, p_is_active, p_sort_order)
    RETURNING id INTO p_id;
END;
$$;
