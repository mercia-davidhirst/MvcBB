/*
    Table:   public.reports
    Purpose: Backing store for MvcBB.Shared.Models.Report.Report /
             IReportRepository.
    Notes:
      - type mirrors ReportType: 0 = Thread, 1 = Post, 2 = Message.
      - status mirrors ReportStatus: 0 = Pending, 1 = Investigating,
        2 = Resolved, 3 = Dismissed.
      - content_id is a polymorphic reference (its meaning depends on type),
        so it cannot be a real foreign key; fn_report_content_exists is used
        by sp_report_insert to validate it against the right table instead.
*/

DROP TABLE IF EXISTS public.reports CASCADE;

CREATE TABLE public.reports
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reason                  VARCHAR(500)    NOT NULL,
    reporter_user_id        INTEGER         NOT NULL REFERENCES public.users (id),
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    type                    SMALLINT        NOT NULL,
    content_id              INTEGER         NOT NULL,
    status                  SMALLINT        NOT NULL DEFAULT 0,
    moderator_notes         VARCHAR(1000)   NOT NULL DEFAULT '',
    resolved_at             TIMESTAMPTZ     NULL,
    resolved_by_user_id     INTEGER         NULL REFERENCES public.users (id),
    CONSTRAINT ck_reports_type CHECK (type IN (0, 1, 2)),
    CONSTRAINT ck_reports_status CHECK (status IN (0, 1, 2, 3))
);

CREATE INDEX ix_reports_created_at ON public.reports (created_at DESC);
CREATE INDEX ix_reports_status ON public.reports (status);
CREATE INDEX ix_reports_type ON public.reports (type);
