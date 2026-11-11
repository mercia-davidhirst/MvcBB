-- Matches IUserRepository.Update(User user)

DROP PROCEDURE IF EXISTS public.sp_user_update(integer, varchar, varchar, varchar, timestamptz, smallint, varchar, varchar, varchar, boolean, varchar);
DROP PROCEDURE IF EXISTS public.sp_user_update(integer, varchar, varchar, varchar, timestamptz, smallint, varchar, varchar, varchar, boolean, varchar, boolean, timestamptz, varchar);

CREATE PROCEDURE public.sp_user_update(
    p_id INTEGER,
    p_username VARCHAR(50),
    p_password_hash VARCHAR(256),
    p_email VARCHAR(256),
    p_last_login_at TIMESTAMPTZ DEFAULT NULL,
    p_role SMALLINT DEFAULT 0,
    p_signature VARCHAR(1000) DEFAULT NULL,
    p_bio VARCHAR(2000) DEFAULT NULL,
    p_avatar_url VARCHAR(500) DEFAULT NULL,
    p_show_email BOOLEAN DEFAULT FALSE,
    p_custom_title VARCHAR(70) DEFAULT NULL,
    p_must_change_password BOOLEAN DEFAULT FALSE,
    p_locked_until TIMESTAMPTZ DEFAULT NULL,
    p_last_login_ip VARCHAR(45) DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.users
    SET username = p_username,
        password_hash = p_password_hash,
        email = p_email,
        last_login_at = p_last_login_at,
        role = p_role,
        signature = p_signature,
        bio = p_bio,
        avatar_url = p_avatar_url,
        show_email = p_show_email,
        custom_title = p_custom_title,
        must_change_password = p_must_change_password,
        locked_until = p_locked_until,
        last_login_ip = p_last_login_ip
    WHERE id = p_id;
END;
$$;
