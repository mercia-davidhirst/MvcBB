/*
    Function: public.fn_role_name
    Purpose:  Converts a users.role SMALLINT (matching
              MvcBB.Shared.Models.User.UserRole) into the display string used
              by MvcBB.Shared.Models.Post.Post.UserRole. Used by
              public.vw_post_details.
*/

-- CREATE OR REPLACE, not DROP + CREATE: vw_post_details depends on this
-- function, so dropping it fails when install.ps1 is re-run on an existing
-- database. Keep the signature and return type the same so this still works.
CREATE OR REPLACE FUNCTION public.fn_role_name(p_role SMALLINT)
RETURNS VARCHAR(20)
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE p_role
        WHEN 0 THEN 'User'
        WHEN 1 THEN 'Moderator'
        WHEN 2 THEN 'Administrator'
        WHEN 3 THEN 'Deleted'
        ELSE NULL
    END;
$$;
