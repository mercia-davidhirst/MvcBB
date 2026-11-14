/*
    Table:   public.thread_subscriptions
    Purpose: Backing store for
             MvcBB.Shared.Models.ThreadSubscription.ThreadSubscriptionModel /
             IThreadSubscriptionRepository - one row per user "watching" a
             thread, created only when they explicitly subscribe (no
             auto-subscribe on reply/create).
    Notes:
      - notify_via_message/notify_via_email are the delivery channels the
        user picked; at least one is normally true, but neither is enforced
        - a subscription with both off is harmless (it just never actually
        notifies - the record still marks "watching").
      - One subscription per (thread_id, user_id) - re-subscribing updates
        the existing row's notify flags instead of creating a duplicate.
*/

CREATE TABLE IF NOT EXISTS public.thread_subscriptions
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    thread_id               INTEGER         NOT NULL REFERENCES public.threads (id),
    user_id                 INTEGER         NOT NULL REFERENCES public.users (id),
    notify_via_message      BOOLEAN         NOT NULL DEFAULT TRUE,
    notify_via_email        BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT uq_thread_subscriptions_thread_user UNIQUE (thread_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_thread_subscriptions_thread_id ON public.thread_subscriptions (thread_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.thread_subscriptions ADD COLUMN IF NOT EXISTS new_column ...;
