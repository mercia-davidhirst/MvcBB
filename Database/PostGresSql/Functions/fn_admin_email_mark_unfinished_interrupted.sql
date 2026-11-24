-- Matches IAdminEmailRepository.MarkUnfinishedInterrupted() - run at API
-- startup: anything still Queued (0) or Sending (1) can't be in progress any
-- more, so it becomes Interrupted (4). A function rather than a procedure
-- only so it can return how many rows it changed.

DROP FUNCTION IF EXISTS public.fn_admin_email_mark_unfinished_interrupted();

CREATE FUNCTION public.fn_admin_email_mark_unfinished_interrupted()
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INTEGER;
BEGIN
    UPDATE public.admin_emails
    SET status = 4,
        completed_at = now(),
        paused_until = NULL
    WHERE status IN (0, 1);

    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$;
