-- Matches IMessageRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_message_get_by_id(integer);

CREATE FUNCTION public.fn_message_get_by_id(p_id INTEGER)
RETURNS SETOF public.messages
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.messages WHERE id = p_id;
$$;
