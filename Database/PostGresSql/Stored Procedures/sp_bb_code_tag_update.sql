-- Matches IBBCodeManagementService.UpdateBBCodeTag(int id, BBCodeTagModel model)

DROP PROCEDURE IF EXISTS public.sp_bb_code_tag_update(integer, varchar, varchar, varchar, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_bb_code_tag_update(
    p_id INTEGER,
    p_name VARCHAR(100),
    p_pattern VARCHAR(500),
    p_replacement VARCHAR(1000),
    p_description VARCHAR(500),
    p_example VARCHAR(500),
    p_is_active BOOLEAN,
    p_sort_order INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.bb_code_tags
    SET name = p_name,
        pattern = p_pattern,
        replacement = p_replacement,
        description = p_description,
        example = p_example,
        is_active = p_is_active,
        sort_order = p_sort_order
    WHERE id = p_id;
END;
$$;
