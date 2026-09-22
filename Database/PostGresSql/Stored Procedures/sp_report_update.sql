-- Matches IReportRepository.Update(Report report)

DROP PROCEDURE IF EXISTS public.sp_report_update(integer, smallint, varchar, timestamptz, integer);

CREATE PROCEDURE public.sp_report_update(
    p_id INTEGER,
    p_status SMALLINT,
    p_moderator_notes VARCHAR(1000),
    p_resolved_at TIMESTAMPTZ DEFAULT NULL,
    p_resolved_by_user_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.reports
    SET status = p_status,
        moderator_notes = p_moderator_notes,
        resolved_at = p_resolved_at,
        resolved_by_user_id = p_resolved_by_user_id
    WHERE id = p_id;
END;
$$;
