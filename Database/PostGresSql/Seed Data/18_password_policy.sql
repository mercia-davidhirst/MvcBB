/*
    Seeds public.password_policy with its one singleton row, matching
    MvcBB.Shared.Models.Settings.PasswordPolicy's own property defaults (the
    same values the old static-field-backed PasswordPolicyService used to
    fall back to). Only runs if the table is still empty, so re-running
    this script won't overwrite values an admin has since changed via
    Admin/Settings.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.password_policy) THEN
        INSERT INTO public.password_policy (id, minimum_length, require_uppercase, require_lowercase, require_digit, require_special_character)
        VALUES
            (1, 8, FALSE, FALSE, FALSE, FALSE);
    END IF;
END;
$$;
