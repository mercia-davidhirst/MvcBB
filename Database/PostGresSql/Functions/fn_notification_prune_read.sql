-- Matches INotificationRepository.PruneRead(DateTime createdBefore)
-- Deletes read notifications older than the cutoff; returns how many went.
-- A function (not a procedure) so the count comes straight back, the same
-- as fn_email_send_log_prune.

DROP FUNCTION IF EXISTS public.fn_notification_prune_read(timestamptz);

CREATE FUNCTION public.fn_notification_prune_read(p_created_before TIMESTAMPTZ)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted INTEGER;
BEGIN
    DELETE FROM public.notifications WHERE is_read = TRUE AND created_at < p_created_before;
    GET DIAGNOSTICS v_deleted = ROW_COUNT;
    RETURN v_deleted;
END;
$$;
