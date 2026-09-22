/*
    View: public.vw_post_details
    Purpose: Reconstructs the full MvcBB.Shared.Models.Post.Post shape,
             including the presentation fields that IPostRepository's
             GetAll/GetByThreadId/GetById results carry alongside the raw row
             (thread_title, created_by_username, updated_by_username,
             deleted_by_username, user_avatar, user_signature,
             user_post_count, user_joined_at, user_role). Used by
             fn_post_get_all, fn_post_get_by_thread_id and fn_post_get_by_id.
    Depends on: public.fn_role_name (Functions/fn_role_name.sql must be
                deployed first).
*/

DROP VIEW IF EXISTS public.vw_post_details CASCADE;

CREATE VIEW public.vw_post_details AS
SELECT
    p.id,
    p.content,
    p.thread_id,
    t.title                            AS thread_title,
    p.created_by_user_id,
    creator.username                   AS created_by_username,
    p.created_at,
    p.is_edited,
    p.edit_reason,
    p.updated_at,
    p.updated_by_user_id,
    updater.username                   AS updated_by_username,
    p.is_deleted,
    p.deleted_by_user_id,
    deleter.username                   AS deleted_by_username,
    p.deleted_at,
    p.delete_reason,
    p.quoted_post_id,
    creator.avatar_url                 AS user_avatar,
    creator.signature                  AS user_signature,
    post_counts.user_post_count,
    creator.created_at                 AS user_joined_at,
    public.fn_role_name(creator.role)  AS user_role
FROM public.posts AS p
INNER JOIN public.threads AS t ON t.id = p.thread_id
INNER JOIN public.users AS creator ON creator.id = p.created_by_user_id
LEFT JOIN public.users AS updater ON updater.id = p.updated_by_user_id
LEFT JOIN public.users AS deleter ON deleter.id = p.deleted_by_user_id
CROSS JOIN LATERAL (
    SELECT COUNT(*) AS user_post_count
    FROM public.posts AS cp
    WHERE cp.created_by_user_id = p.created_by_user_id
      AND cp.is_deleted = FALSE
) AS post_counts;
