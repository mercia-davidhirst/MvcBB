-- Matches IBBCodeManagementService.AddSmilie(SmilieModel model)

DROP PROCEDURE IF EXISTS public.sp_smilie_insert(varchar, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_smilie_insert(
    p_code VARCHAR(20),
    p_description VARCHAR(200) DEFAULT '',
    p_image_path VARCHAR(500) DEFAULT '',
    p_is_active BOOLEAN DEFAULT TRUE,
    p_sort_order INTEGER DEFAULT 0,
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
