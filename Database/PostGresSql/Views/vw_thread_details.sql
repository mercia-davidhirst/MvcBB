/*
    View: public.vw_thread_details
    Purpose: Reconstructs the full MvcBB.Shared.Models.ForumThread.ForumThread
             shape, including the presentation fields that
             IForumThreadRepository's GetAll/GetByBoardId/GetById/GetPage
             results carry alongside the raw row (board_name, board_is_private,
             created_by_username, last_post_by_username) - matches the same
             pattern vw_post_details already uses for Post. Used by
             fn_thread_get_all, fn_thread_get_by_board_id, fn_thread_get_by_id,
             fn_thread_get_page and fn_thread_get_page_count.
*/

DROP VIEW IF EXISTS public.vw_thread_details CASCADE;

CREATE VIEW public.vw_thread_details AS
SELECT
    t.id,
    t.title,
    t.board_id,
    b.name::text                   AS board_name,
    b.is_private                   AS board_is_private,
    t.created_by_user_id,
    creator.username::text         AS created_by_username,
    t.created_at,
    t.updated_at,
    t.is_sticky,
    t.is_locked,
    t.view_count,
    t.post_count,
    t.last_post_at,
    t.last_post_by_user_id,
    lastPoster.username::text      AS last_post_by_username
FROM public.threads AS t
INNER JOIN public.boards AS b ON b.id = t.board_id
INNER JOIN public.users AS creator ON creator.id = t.created_by_user_id
LEFT JOIN public.users AS lastPoster ON lastPoster.id = t.last_post_by_user_id;
