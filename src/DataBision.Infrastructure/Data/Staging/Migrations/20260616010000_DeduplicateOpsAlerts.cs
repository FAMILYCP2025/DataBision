using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Corrective migration: ops.evaluate_alert_rules was inserting a new alert_event row
    /// on every evaluation even when an identical unresolved alert already existed.
    /// This caused alert_event to grow unboundedly across transform runs.
    /// Fix: add NOT EXISTS guard before each INSERT so that a rule that is already
    /// active (is_resolved = FALSE) for the same company_id + rule_code does not produce
    /// a duplicate row. Existing history is preserved; only future duplicates are prevented.
    /// </summary>
    public partial class DeduplicateOpsAlerts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

                    IF (v_last_run IS NULL OR v_last_run < NOW() - INTERVAL '24 hours')
                       AND NOT EXISTS (
                           SELECT 1 FROM ops.alert_event
                           WHERE company_id = p_company_id
                             AND rule_code = 'EXTRACTOR_NOT_RUN_RECENTLY'
                             AND is_resolved = FALSE
                       ) THEN
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
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM ops.alert_event
                        WHERE company_id = p_company_id
                          AND rule_code = 'TRANSFORM_FAILED'
                          AND is_resolved = FALSE
                    ) THEN
                        INSERT INTO ops.alert_event (company_id, rule_id, rule_code, severity, message)
                        SELECT p_company_id, id, rule_code, severity, 'El último transform finalizó en ERROR'
                        FROM ops.alert_rule WHERE rule_code = 'TRANSFORM_FAILED' AND is_active;
                        v_triggered := v_triggered + 1;
                    END IF;

                    -- DATA_QUALITY_ERRORS
                    IF (SELECT COUNT(*) FROM ops.data_quality_issue
                        WHERE company_id = p_company_id AND is_resolved = FALSE) > 5
                    AND NOT EXISTS (
                        SELECT 1 FROM ops.alert_event
                        WHERE company_id = p_company_id
                          AND rule_code = 'DATA_QUALITY_ERRORS'
                          AND is_resolved = FALSE
                    ) THEN
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
                               WHERE table_schema = 'mart' AND table_name = 'finance_executive_daily')
                    AND EXISTS (
                        SELECT 1 FROM mart.finance_executive_daily
                        WHERE company_id = p_company_id AND ar_overdue_pct > 0.30
                        ORDER BY period_date DESC LIMIT 1
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM ops.alert_event
                        WHERE company_id = p_company_id
                          AND rule_code = 'AR_OVERDUE_HIGH'
                          AND is_resolved = FALSE
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

                    PERFORM ops.refresh_pipeline_health(p_company_id);
                    RETURN v_triggered;
                END;
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore original version without deduplication guard
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ops.evaluate_alert_rules(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE
                    v_triggered INT := 0;
                    v_last_run  TIMESTAMPTZ;
                BEGIN
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
        }
    }
}
