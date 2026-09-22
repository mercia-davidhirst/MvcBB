-- Matches IMessageRepository.GetAll()

DROP FUNCTION IF EXISTS public.fn_message_get_all();

CREATE FUNCTION public.fn_message_get_all()
RETURNS SETOF public.messages
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.messages;
$$;
