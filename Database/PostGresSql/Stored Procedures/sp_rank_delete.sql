-- Matches IRankRepository.Remove(RankModel rank)

DROP PROCEDURE IF EXISTS public.sp_rank_delete(integer);

CREATE PROCEDURE public.sp_rank_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.ranks WHERE id = p_id;
END;
$$;
