-- Matches IPasswordPolicyRepository.Get()

DROP FUNCTION IF EXISTS public.fn_password_policy_get();

CREATE FUNCTION public.fn_password_policy_get()
RETURNS public.password_policy
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.password_policy WHERE id = 1;
$$;
