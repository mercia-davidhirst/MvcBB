-- Matches IUserRepository.Add(User user)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).
-- Invoke with: CALL sp_user_insert('alice', '<hash>', 'alice@example.com', 0, NULL, NULL, NULL, FALSE, NULL, NULL);
-- p_id comes back populated with the generated identity.

DROP PROCEDURE IF EXISTS public.sp_user_insert(varchar, varchar, varchar, smallint, varchar, varchar, varchar, boolean, varchar);
DROP PROCEDURE IF EXISTS public.sp_user_insert(varchar, varchar, varchar, smallint, varchar, varchar, varchar, boolean, varchar, boolean, timestamptz, varchar);
DROP PROCEDURE IF EXISTS public.sp_user_insert(varchar, varchar, varchar, smallint, varchar, varchar, varchar, boolean, varchar, boolean, timestamptz, varchar, boolean);

CREATE PROCEDURE public.sp_user_insert(
    p_username VARCHAR(50),
    p_password_hash VARCHAR(256),
    p_email VARCHAR(256),
    p_role SMALLINT,
    p_signature VARCHAR(1000),
    p_bio VARCHAR(2000),
    p_avatar_url VARCHAR(500),
    p_show_email BOOLEAN,
    p_custom_title VARCHAR(70),
    p_must_change_password BOOLEAN,
    p_locked_until TIMESTAMPTZ,
    p_last_login_ip VARCHAR(45),
    p_auto_jump_to_unread BOOLEAN,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.users (username, password_hash, email, role, signature, bio, avatar_url, show_email, custom_title, must_change_password, locked_until, last_login_ip, auto_jump_to_unread)
    VALUES (p_username, p_password_hash, p_email, p_role, p_signature, p_bio, p_avatar_url, p_show_email, p_custom_title, p_must_change_password, p_locked_until, p_last_login_ip, p_auto_jump_to_unread)
    RETURNING id INTO p_id;
END;
$$;
