-- Matches IBBCodeManagementService.DeleteBBCodeTag(int id)

DROP PROCEDURE IF EXISTS public.sp_bb_code_tag_delete(integer);

CREATE PROCEDURE public.sp_bb_code_tag_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.bb_code_tags WHERE id = p_id;
END;
$$;
