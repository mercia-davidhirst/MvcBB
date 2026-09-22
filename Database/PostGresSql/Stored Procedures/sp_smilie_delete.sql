-- Matches IBBCodeManagementService.DeleteSmilie(int id)

DROP PROCEDURE IF EXISTS public.sp_smilie_delete(integer);

CREATE PROCEDURE public.sp_smilie_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.smilies WHERE id = p_id;
END;
$$;
