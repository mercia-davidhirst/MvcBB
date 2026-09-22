-- Matches IBBCodeManagementService.UpdateSmilie(int id, SmilieModel model)

DROP PROCEDURE IF EXISTS public.sp_smilie_update(integer, varchar, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_smilie_update(
    p_id INTEGER,
    p_code VARCHAR(20),
    p_description VARCHAR(200),
    p_image_path VARCHAR(500),
    p_is_active BOOLEAN,
    p_sort_order INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.smilies
    SET code = p_code,
        description = p_description,
        image_path = p_image_path,
        is_active = p_is_active,
        sort_order = p_sort_order
    WHERE id = p_id;
END;
$$;
