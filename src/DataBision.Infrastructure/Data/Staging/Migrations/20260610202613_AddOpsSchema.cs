using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class AddOpsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS ops;");

            // ── ops.extractor_run ──────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.extractor_run (
                    id                  BIGSERIAL       PRIMARY KEY,
                    company_id          TEXT            NOT NULL,
                    sap_object          TEXT            NOT NULL,
                    started_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    finished_at_utc     TIMESTAMPTZ,
                    status              TEXT            NOT NULL DEFAULT 'RUNNING',
                    pages_fetched       INTEGER         NOT NULL DEFAULT 0,
                    rows_extracted      INTEGER         NOT NULL DEFAULT 0,
                    rows_inserted       INTEGER         NOT NULL DEFAULT 0,
                    rows_updated        INTEGER         NOT NULL DEFAULT 0,
                    hit_max_pages       BOOLEAN         NOT NULL DEFAULT FALSE,
                    last_error          TEXT,
                    watermark_date      TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_ops_extractor_run_company_obj
                    ON ops.extractor_run (company_id, sap_object, started_at_utc DESC);
                """);

            // ── ops.extractor_page_log ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.extractor_page_log (
                    id              BIGSERIAL   PRIMARY KEY,
                    run_id          BIGINT      NOT NULL REFERENCES ops.extractor_run(id) ON DELETE CASCADE,
                    sap_object      TEXT        NOT NULL,
                    page_number     INTEGER     NOT NULL,
                    skip_offset     INTEGER     NOT NULL DEFAULT 0,
                    top_count       INTEGER     NOT NULL,
                    rows_received   INTEGER     NOT NULL DEFAULT 0,
                    elapsed_ms      BIGINT      NOT NULL DEFAULT 0,
                    status          TEXT        NOT NULL DEFAULT 'OK',
                    error_code      TEXT,
                    error_message   TEXT,
                    logged_at_utc   TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS idx_ops_page_log_run
                    ON ops.extractor_page_log (run_id, page_number);
                """);

            // ── ops.transform_run ──────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.transform_run (
                    id                  BIGSERIAL   PRIMARY KEY,
                    company_id          TEXT        NOT NULL,
                    transform_type      TEXT        NOT NULL,
                    started_at_utc      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    finished_at_utc     TIMESTAMPTZ,
                    status              TEXT        NOT NULL DEFAULT 'RUNNING',
                    objects_refreshed   INTEGER     NOT NULL DEFAULT 0,
                    last_error          TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_ops_transform_run_company
                    ON ops.transform_run (company_id, started_at_utc DESC);
                """);

            // ── ops.data_quality_issue ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.data_quality_issue (
                    id              BIGSERIAL   PRIMARY KEY,
                    company_id      TEXT        NOT NULL,
                    sap_object      TEXT        NOT NULL,
                    issue_type      TEXT        NOT NULL,
                    severity        TEXT        NOT NULL DEFAULT 'WARNING',
                    description     TEXT        NOT NULL,
                    affected_rows   INTEGER     NOT NULL DEFAULT 0,
                    sample_key      TEXT,
                    detected_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    resolved_at_utc TIMESTAMPTZ,
                    is_resolved     BOOLEAN     NOT NULL DEFAULT FALSE
                );
                CREATE INDEX IF NOT EXISTS idx_ops_dq_company_unresolved
                    ON ops.data_quality_issue (company_id, detected_at_utc DESC)
                    WHERE is_resolved = FALSE;
                """);

            // ── ops.alert_rule ─────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.alert_rule (
                    id                  SERIAL      PRIMARY KEY,
                    rule_code           TEXT        NOT NULL UNIQUE,
                    rule_name           TEXT        NOT NULL,
                    description         TEXT,
                    severity            TEXT        NOT NULL DEFAULT 'WARNING',
                    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
                    threshold_value     NUMERIC,
                    threshold_unit      TEXT,
                    check_query         TEXT,
                    created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                """);

            // ── ops.alert_event ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.alert_event (
                    id               BIGSERIAL   PRIMARY KEY,
                    company_id       TEXT        NOT NULL,
                    rule_id          INTEGER     NOT NULL REFERENCES ops.alert_rule(id),
                    rule_code        TEXT        NOT NULL,
                    severity         TEXT        NOT NULL,
                    triggered_value  TEXT,
                    message          TEXT,
                    triggered_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    resolved_at_utc  TIMESTAMPTZ,
                    is_resolved      BOOLEAN     NOT NULL DEFAULT FALSE
                );
                CREATE INDEX IF NOT EXISTS idx_ops_alert_event_company
                    ON ops.alert_event (company_id, triggered_at_utc DESC)
                    WHERE is_resolved = FALSE;
                """);

            // ── ops.pipeline_health ────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ops.pipeline_health (
                    company_id              TEXT        PRIMARY KEY,
                    last_extractor_run_utc  TIMESTAMPTZ,
                    last_transform_run_utc  TIMESTAMPTZ,
                    extractor_status        TEXT        NOT NULL DEFAULT 'UNKNOWN',
                    transform_status        TEXT        NOT NULL DEFAULT 'UNKNOWN',
                    active_alerts           INTEGER     NOT NULL DEFAULT 0,
                    dq_errors_unresolved    INTEGER     NOT NULL DEFAULT 0,
                    objects_extracted       INTEGER     NOT NULL DEFAULT 0,
                    last_error              TEXT,
                    health_score            INTEGER     NOT NULL DEFAULT 0,
                    updated_at_utc          TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                """);

            // ── alert_rule seeds (8 reglas) ────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO ops.alert_rule (rule_code, rule_name, description, severity, threshold_value, threshold_unit)
                VALUES
                    ('EXTRACTOR_NOT_RUN_RECENTLY',
                     'Extractor no ejecutado',
                     'El extractor no ha corrido en las últimas 24 horas.',
                     'ERROR', 24, 'HOURS'),
                    ('MART_EMPTY',
                     'MART sin datos',
                     'Una tabla MART principal tiene 0 filas para la empresa.',
                     'WARNING', 0, 'ROWS'),
                    ('STG_EMPTY',
                     'STG sin datos',
                     'Una tabla STG principal tiene 0 filas para la empresa.',
                     'WARNING', 0, 'ROWS'),
                    ('SALES_DROP_DAILY',
                     'Caída ventas diaria',
                     'Las ventas del día cayeron más del 50% respecto al promedio de 30 días.',
                     'WARNING', 50, 'PERCENT'),
                    ('STOCKOUT_ITEMS',
                     'Ítems sin stock',
                     'El número de ítems con stock <= 0 supera el umbral.',
                     'WARNING', 10, 'ROWS'),
                    ('AR_OVERDUE_HIGH',
                     'Mora CxC alta',
                     'El porcentaje de CxC vencida supera el 30%.',
                     'WARNING', 30, 'PERCENT'),
                    ('DATA_QUALITY_ERRORS',
                     'Errores de calidad de datos',
                     'Hay más de 5 errores de calidad sin resolver.',
                     'WARNING', 5, 'ROWS'),
                    ('TRANSFORM_FAILED',
                     'Transform fallido',
                     'El último transform_run terminó en estado ERROR.',
                     'ERROR', NULL, NULL)
                ON CONFLICT (rule_code) DO NOTHING;
                """);

            // ── ops.refresh_pipeline_health(company_id) ────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ops.refresh_pipeline_health(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                DECLARE
                    v_last_ext      TIMESTAMPTZ;
                    v_ext_status    TEXT;
                    v_last_xform    TIMESTAMPTZ;
                    v_xform_status  TEXT;
                    v_active_alerts INTEGER;
                    v_dq_errors     INTEGER;
                    v_obj_count     INTEGER;
                    v_score         INTEGER;
                BEGIN
                    SELECT MAX(started_at_utc),
                           (SELECT status FROM ops.extractor_run
                            WHERE company_id = p_company_id ORDER BY started_at_utc DESC LIMIT 1)
                    INTO v_last_ext, v_ext_status
                    FROM ops.extractor_run WHERE company_id = p_company_id;

                    v_ext_status := COALESCE(v_ext_status, 'NEVER_RUN');

                    SELECT MAX(started_at_utc),
                           (SELECT status FROM ops.transform_run
                            WHERE company_id = p_company_id ORDER BY started_at_utc DESC LIMIT 1)
                    INTO v_last_xform, v_xform_status
                    FROM ops.transform_run WHERE company_id = p_company_id;

                    v_xform_status := COALESCE(v_xform_status, 'NEVER_RUN');

                    SELECT COUNT(*) INTO v_active_alerts
                    FROM ops.alert_event
                    WHERE company_id = p_company_id AND is_resolved = FALSE;

                    SELECT COUNT(*) INTO v_dq_errors
                    FROM ops.data_quality_issue
                    WHERE company_id = p_company_id AND is_resolved = FALSE;

                    SELECT COUNT(DISTINCT sap_object) INTO v_obj_count
                    FROM ops.extractor_run
                    WHERE company_id = p_company_id AND status = 'SUCCESS';

                    v_score := 100;
                    IF v_ext_status IN ('NEVER_RUN', 'ERROR') THEN v_score := v_score - 40; END IF;
                    IF v_xform_status IN ('NEVER_RUN', 'ERROR') THEN v_score := v_score - 30; END IF;
                    IF v_active_alerts > 0 THEN v_score := v_score - LEAST(v_active_alerts * 5, 20); END IF;
                    IF v_dq_errors > 0 THEN v_score := v_score - LEAST(v_dq_errors * 2, 10); END IF;
                    v_score := GREATEST(v_score, 0);

                    INSERT INTO ops.pipeline_health (
                        company_id, last_extractor_run_utc, last_transform_run_utc,
                        extractor_status, transform_status,
                        active_alerts, dq_errors_unresolved, objects_extracted,
                        health_score, updated_at_utc
                    ) VALUES (
                        p_company_id, v_last_ext, v_last_xform,
                        v_ext_status, v_xform_status,
                        v_active_alerts, v_dq_errors, v_obj_count,
                        v_score, NOW()
                    )
                    ON CONFLICT (company_id) DO UPDATE SET
                        last_extractor_run_utc  = EXCLUDED.last_extractor_run_utc,
                        last_transform_run_utc  = EXCLUDED.last_transform_run_utc,
                        extractor_status        = EXCLUDED.extractor_status,
                        transform_status        = EXCLUDED.transform_status,
                        active_alerts           = EXCLUDED.active_alerts,
                        dq_errors_unresolved    = EXCLUDED.dq_errors_unresolved,
                        objects_extracted       = EXCLUDED.objects_extracted,
                        health_score            = EXCLUDED.health_score,
                        updated_at_utc          = NOW();
                END;
                $$;
                """);

            // ── ops.evaluate_alert_rules(company_id) ───────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ops.evaluate_alert_rules(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE
                    v_triggered INT := 0;
                    v_last_run  TIMESTAMPTZ;
                BEGIN
                    -- EXTRACTOR_NOT_RUN_RECENTLY
                    SELECT MAX(started_at_utc) INTO v_last_run
                    FROM ops.extractor_run WHERE company_id = p_company_id;

                    IF v_last_run IS NULL OR v_last_run < NOW() - INTERVAL '24 hours' THEN
                        INSERT INTO ops.alert_event (company_id, rule_id, rule_code, severity, triggered_value, message)
                        SELECT p_company_id, id, rule_code, severity,
                               COALESCE(ROUND(EXTRACT(EPOCH FROM NOW() - v_last_run)/3600, 1)::TEXT || 'h', 'NEVER'),
                               'Extractor no ejecutado en las últimas 24 horas'
                        FROM ops.alert_rule WHERE rule_code = 'EXTRACTOR_NOT_RUN_RECENTLY' AND is_active;
                        v_triggered := v_triggered + 1;
                    END IF;

                    -- TRANSFORM_FAILED
                    IF EXISTS (
                        SELECT 1 FROM ops.transform_run
                        WHERE company_id = p_company_id AND status = 'ERROR'
                          AND started_at_utc > NOW() - INTERVAL '48 hours'
                    ) THEN
                        INSERT INTO ops.alert_event (company_id, rule_id, rule_code, severity, message)
                        SELECT p_company_id, id, rule_code, severity, 'El último transform finalizó en ERROR'
                        FROM ops.alert_rule WHERE rule_code = 'TRANSFORM_FAILED' AND is_active;
                        v_triggered := v_triggered + 1;
                    END IF;

                    -- DATA_QUALITY_ERRORS
                    IF (SELECT COUNT(*) FROM ops.data_quality_issue
                        WHERE company_id = p_company_id AND is_resolved = FALSE) > 5 THEN
                        INSERT INTO ops.alert_event (company_id, rule_id, rule_code, severity, triggered_value, message)
                        SELECT p_company_id, id, rule_code, severity,
                               (SELECT COUNT(*)::TEXT FROM ops.data_quality_issue
                                WHERE company_id = p_company_id AND is_resolved = FALSE),
                               'Hay más de 5 errores de calidad de datos sin resolver'
                        FROM ops.alert_rule WHERE rule_code = 'DATA_QUALITY_ERRORS' AND is_active;
                        v_triggered := v_triggered + 1;
                    END IF;

                    -- AR_OVERDUE_HIGH (si MART finance disponible)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema = 'mart' AND table_name = 'finance_executive_daily') THEN
                        IF EXISTS (
                            SELECT 1 FROM mart.finance_executive_daily
                            WHERE company_id = p_company_id AND ar_overdue_pct > 0.30
                            ORDER BY period_date DESC LIMIT 1
                        ) THEN
                            INSERT INTO ops.alert_event (company_id, rule_id, rule_code, severity, triggered_value, message)
                            SELECT p_company_id, id, rule_code, severity,
                                   (SELECT ROUND(ar_overdue_pct * 100, 1)::TEXT || '%'
                                    FROM mart.finance_executive_daily
                                    WHERE company_id = p_company_id
                                    ORDER BY period_date DESC LIMIT 1),
                                   'CxC vencida supera el 30%'
                            FROM ops.alert_rule WHERE rule_code = 'AR_OVERDUE_HIGH' AND is_active;
                            v_triggered := v_triggered + 1;
                        END IF;
                    END IF;

                    PERFORM ops.refresh_pipeline_health(p_company_id);
                    RETURN v_triggered;
                END;
                $$;
                """);

            // ── ops.log_data_quality_issue(...) ────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ops.log_data_quality_issue(
                    p_company_id    TEXT,
                    p_sap_object    TEXT,
                    p_issue_type    TEXT,
                    p_severity      TEXT,
                    p_description   TEXT,
                    p_affected_rows INTEGER DEFAULT 0,
                    p_sample_key    TEXT    DEFAULT NULL
                )
                RETURNS BIGINT LANGUAGE plpgsql AS $$
                DECLARE v_id BIGINT;
                BEGIN
                    INSERT INTO ops.data_quality_issue (
                        company_id, sap_object, issue_type, severity,
                        description, affected_rows, sample_key
                    ) VALUES (
                        p_company_id, p_sap_object, p_issue_type, p_severity,
                        p_description, p_affected_rows, p_sample_key
                    )
                    RETURNING id INTO v_id;
                    RETURN v_id;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS ops.log_data_quality_issue(TEXT,TEXT,TEXT,TEXT,TEXT,INTEGER,TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS ops.evaluate_alert_rules(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS ops.refresh_pipeline_health(TEXT);");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.pipeline_health;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.alert_event;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.alert_rule;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.data_quality_issue;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.transform_run;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.extractor_page_log;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ops.extractor_run;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS ops;");
        }
    }
}
