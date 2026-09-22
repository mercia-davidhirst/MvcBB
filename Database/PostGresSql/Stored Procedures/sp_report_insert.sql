-- Matches IReportRepository.Add(Report report)
-- Validates the polymorphic content_id/type pair via
-- public.fn_report_content_exists before inserting, since no ordinary
-- foreign key can express that relationship.

DROP PROCEDURE IF EXISTS public.sp_report_insert(varchar, integer, smallint, integer, smallint, varchar);

CREATE PROCEDURE public.sp_report_insert(
    p_reason VARCHAR(500),
    p_reporter_user_id INTEGER,
    p_type SMALLINT,
    p_content_id INTEGER,
    p_status SMALLINT DEFAULT 0,
    p_moderator_notes VARCHAR(1000) DEFAULT '',
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT public.fn_report_content_exists(p_type, p_content_id) THEN
        RAISE EXCEPTION 'content_id % does not exist for report type %', p_content_id, p_type;
    END IF;

    INSERT INTO public.reports (reason, reporter_user_id, type, content_id, status, moderator_notes)
    VALUES (p_reason, p_reporter_user_id, p_type, p_content_id, p_status, p_moderator_notes)
    RETURNING id INTO p_id;
END;
$$;
