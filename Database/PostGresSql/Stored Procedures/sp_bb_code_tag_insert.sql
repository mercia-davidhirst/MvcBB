-- Matches IBBCodeManagementService.AddBBCodeTag(BBCodeTagModel model)

DROP PROCEDURE IF EXISTS public.sp_bb_code_tag_insert(varchar, varchar, varchar, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_bb_code_tag_insert(
    p_name VARCHAR(100),
    p_pattern VARCHAR(500),
    p_replacement VARCHAR(1000),
    p_description VARCHAR(500) DEFAULT '',
    p_example VARCHAR(500) DEFAULT '',
    p_is_active BOOLEAN DEFAULT TRUE,
    p_sort_order INTEGER DEFAULT 0,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.bb_code_tags (name, pattern, replacement, description, example, is_active, sort_order)
    VALUES (p_name, p_pattern, p_replacement, p_description, p_example, p_is_active, p_sort_order)
    RETURNING id INTO p_id;
END;
$$;
