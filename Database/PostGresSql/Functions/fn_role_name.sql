/*
    Function: public.fn_role_name
    Purpose:  Converts a users.role SMALLINT (matching
              MvcBB.Shared.Models.User.UserRole) into the display string used
              by MvcBB.Shared.Models.Post.Post.UserRole. Used by
              public.vw_post_details.
*/

DROP FUNCTION IF EXISTS public.fn_role_name(smallint);

CREATE FUNCTION public.fn_role_name(p_role SMALLINT)
RETURNS VARCHAR(20)
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE p_role
        WHEN 0 THEN 'User'
        WHEN 1 THEN 'Moderator'
        WHEN 2 THEN 'Administrator'
        ELSE NULL
    END;
$$;
