-- Matches IUserRepository.Add(User user)
-- Invoke with: CALL sp_user_insert('alice', '<hash>', 'alice@example.com', 0, NULL, NULL, NULL, FALSE, NULL, NULL);
-- p_id comes back populated with the generated identity.

DROP PROCEDURE IF EXISTS public.sp_user_insert(varchar, varchar, varchar, smallint, varchar, varchar, varchar, boolean, varchar);
DROP PROCEDURE IF EXISTS public.sp_user_insert(varchar, varchar, varchar, smallint, varchar, varchar, varchar, boolean, varchar, boolean, timestamptz, varchar);

CREATE PROCEDURE public.sp_user_insert(
    p_username VARCHAR(50),
    p_password_hash VARCHAR(256),
    p_email VARCHAR(256),
    p_role SMALLINT DEFAULT 0,
    p_signature VARCHAR(1000) DEFAULT NULL,
    p_bio VARCHAR(2000) DEFAULT NULL,
    p_avatar_url VARCHAR(500) DEFAULT NULL,
    p_show_email BOOLEAN DEFAULT FALSE,
    p_custom_title VARCHAR(70) DEFAULT NULL,
    p_must_change_password BOOLEAN DEFAULT FALSE,
    p_locked_until TIMESTAMPTZ DEFAULT NULL,
    p_last_login_ip VARCHAR(45) DEFAULT NULL,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.users (username, password_hash, email, role, signature, bio, avatar_url, show_email, custom_title, must_change_password, locked_until, last_login_ip)
    VALUES (p_username, p_password_hash, p_email, p_role, p_signature, p_bio, p_avatar_url, p_show_email, p_custom_title, p_must_change_password, p_locked_until, p_last_login_ip)
    RETURNING id INTO p_id;
END;
$$;
