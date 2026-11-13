-- Matches IPasswordPolicyRepository.Update(PasswordPolicy policy)

DROP PROCEDURE IF EXISTS public.sp_password_policy_update(integer, boolean, boolean, boolean, boolean);

CREATE PROCEDURE public.sp_password_policy_update(
    p_minimum_length INTEGER,
    p_require_uppercase BOOLEAN,
    p_require_lowercase BOOLEAN,
    p_require_digit BOOLEAN,
    p_require_special_character BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.password_policy
    SET minimum_length = p_minimum_length,
        require_uppercase = p_require_uppercase,
        require_lowercase = p_require_lowercase,
        require_digit = p_require_digit,
        require_special_character = p_require_special_character
    WHERE id = 1;
END;
$$;
