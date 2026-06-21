using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint 19: Productivizes the PCGE Peru financial MART.
    ///
    /// Changes:
    ///   mart.refresh_income_statement  — PCGE Peru sign convention applied:
    ///       revenue/other_income/financial = credit - debit (positive = income)
    ///       cogs/tax = ABS(debit - credit) (always positive cost)
    ///
    ///   mart.refresh_ebitda             — ABS applied to cogs reads; financial treated as income (+);
    ///       tax treated as cost (ABS); net_income = revenue - cogs - opex + financial - tax
    ///
    ///   mart.refresh_gl_accounts_from_journal_lines (NEW)
    ///       Injects JDT1 orphan accounts (posting accounts in JDT1 not in OACT/stg.gl_account)
    ///       into mart.gl_accounts with PCGE prefix-based classification.
    ///       Idempotent: ON CONFLICT DO UPDATE reclassifies on each run.
    ///
    ///   mart.refresh_accounting_all    — Now calls refresh_gl_accounts_from_journal_lines
    ///       immediately after refresh_gl_accounts, guaranteeing JDT1 accounts survive every refresh.
    ///       Call order: stg.refresh_gl_accounts → stg.refresh_journal_entries →
    ///           mart.refresh_gl_accounts → mart.refresh_gl_accounts_from_journal_lines →
    ///           mart.refresh_account_balances → mart.refresh_income_statement →
    ///           mart.refresh_balance_sheet → mart.refresh_ebitda
    ///
    /// All functions use CREATE OR REPLACE — safe to re-run; idempotent.
    /// </summary>
    public partial class Sprint19AccountingMartPcgeFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── mart.refresh_gl_accounts_from_journal_lines (NEW — 19C) ──────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_gl_accounts_from_journal_lines(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $func$
                BEGIN
                    INSERT INTO mart.gl_accounts (
                        company_id, code, name, father_num, level, account_type,
                        statement_line, postable, currency, refreshed_at
                    )
                    SELECT
                        p_company_id,
                        jl.account                                   AS code,
                        'JDT1:' || jl.account                       AS name,
                        NULL, NULL, NULL,
                        COALESCE(
                            (SELECT r.statement_line FROM cfg.account_classification_rules r
                             WHERE r.company_id = p_company_id AND r.account_code = jl.account LIMIT 1),
                            (SELECT r.statement_line FROM cfg.account_classification_rules r
                             WHERE r.company_id = p_company_id AND r.account_code IS NULL
                               AND r.format_code IS NOT NULL
                               AND jl.account LIKE r.format_code || '%'
                             ORDER BY LENGTH(r.format_code) DESC LIMIT 1),
                            'unclassified'
                        )                                            AS statement_line,
                        FALSE,
                        NULL,
                        NOW()
                    FROM (
                        SELECT DISTINCT account
                        FROM stg.journal_entry_line
                        WHERE company_id = p_company_id AND account IS NOT NULL
                    ) jl
                    WHERE NOT EXISTS (
                        SELECT 1 FROM stg.gl_account g
                        WHERE g.company_id = p_company_id AND g.code = jl.account
                    )
                    ON CONFLICT (company_id, code) DO UPDATE SET
                        statement_line = EXCLUDED.statement_line,
                        refreshed_at   = NOW();
                END;
                $func$;
                """);

            // ── mart.refresh_income_statement (19A — PCGE sign convention) ────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_income_statement(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $func$
                BEGIN
                    INSERT INTO mart.income_statement_summary (
                        company_id, period_year, period_month, statement_line, amount, refreshed_at
                    )
                    SELECT
                        ab.company_id, ab.period_year, ab.period_month,
                        COALESCE(ga.statement_line, 'unclassified') AS statement_line,
                        CASE COALESCE(ga.statement_line, 'unclassified')
                            WHEN 'revenue'       THEN SUM(ab.credit_sum - ab.debit_sum)
                            WHEN 'other_income'  THEN SUM(ab.credit_sum - ab.debit_sum)
                            WHEN 'financial'     THEN SUM(ab.credit_sum - ab.debit_sum)
                            WHEN 'cogs'          THEN ABS(SUM(ab.debit_sum - ab.credit_sum))
                            WHEN 'opex'          THEN SUM(ab.debit_sum  - ab.credit_sum)
                            WHEN 'other_expense' THEN SUM(ab.debit_sum  - ab.credit_sum)
                            WHEN 'tax'           THEN ABS(SUM(ab.debit_sum - ab.credit_sum))
                            ELSE                      SUM(ab.credit_sum - ab.debit_sum)
                        END AS amount,
                        NOW()
                    FROM mart.account_balances ab
                    LEFT JOIN mart.gl_accounts ga ON ga.company_id = ab.company_id AND ga.code = ab.code
                    WHERE ab.company_id = p_company_id
                      AND COALESCE(ga.statement_line, 'unclassified') IN (
                          'revenue','cogs','opex','other_income','other_expense','financial','tax','unclassified')
                    GROUP BY ab.company_id, ab.period_year, ab.period_month, COALESCE(ga.statement_line, 'unclassified')
                    ON CONFLICT (company_id, period_year, period_month, statement_line) DO UPDATE SET
                        amount = EXCLUDED.amount, refreshed_at = NOW();
                END;
                $func$;
                """);

            // ── mart.refresh_ebitda (19B — ABS cogs, + financial in net_income) ──
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_ebitda(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $func$
                BEGIN
                    INSERT INTO mart.ebitda_summary (
                        company_id, period_year, period_month,
                        revenue, cogs, gross_profit, opex, ebitda,
                        depreciation, amortization, financial_result, tax_result, net_income,
                        refreshed_at
                    )
                    SELECT
                        company_id, period_year, period_month,
                        MAX(CASE WHEN statement_line='revenue'   THEN COALESCE(amount,0) ELSE 0 END),
                        MAX(CASE WHEN statement_line='cogs'      THEN ABS(COALESCE(amount,0)) ELSE 0 END),
                        MAX(CASE WHEN statement_line='revenue'   THEN COALESCE(amount,0) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='cogs'    THEN ABS(COALESCE(amount,0)) ELSE 0 END),
                        MAX(CASE WHEN statement_line='opex'      THEN COALESCE(amount,0) ELSE 0 END),
                        MAX(CASE WHEN statement_line='revenue'   THEN COALESCE(amount,0) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='cogs'    THEN ABS(COALESCE(amount,0)) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='opex'    THEN COALESCE(amount,0) ELSE 0 END),
                        0, 0,
                        MAX(CASE WHEN statement_line='financial' THEN COALESCE(amount,0) ELSE 0 END),
                        MAX(CASE WHEN statement_line='tax'       THEN ABS(COALESCE(amount,0)) ELSE 0 END),
                        MAX(CASE WHEN statement_line='revenue'   THEN COALESCE(amount,0) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='cogs'    THEN ABS(COALESCE(amount,0)) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='opex'    THEN COALESCE(amount,0) ELSE 0 END)
                        + MAX(CASE WHEN statement_line='financial' THEN COALESCE(amount,0) ELSE 0 END)
                        - MAX(CASE WHEN statement_line='tax'     THEN ABS(COALESCE(amount,0)) ELSE 0 END),
                        NOW()
                    FROM mart.income_statement_summary
                    WHERE company_id = p_company_id
                    GROUP BY company_id, period_year, period_month
                    ON CONFLICT (company_id, period_year, period_month) DO UPDATE SET
                        revenue=EXCLUDED.revenue, cogs=EXCLUDED.cogs, gross_profit=EXCLUDED.gross_profit,
                        opex=EXCLUDED.opex, ebitda=EXCLUDED.ebitda,
                        depreciation=EXCLUDED.depreciation, amortization=EXCLUDED.amortization,
                        financial_result=EXCLUDED.financial_result, tax_result=EXCLUDED.tax_result,
                        net_income=EXCLUDED.net_income, refreshed_at=NOW();
                END;
                $func$;
                """);

            // ── mart.refresh_accounting_all (19E — add JDT1 enrichment step) ─────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_accounting_all(p_company_id TEXT)
                RETURNS TABLE(step_name TEXT, status TEXT, executed_at_utc TIMESTAMPTZ, message TEXT)
                LANGUAGE plpgsql AS $func$
                DECLARE
                    v_step TEXT; v_status TEXT; v_ts TIMESTAMPTZ; v_msg TEXT;
                BEGIN
                    v_step := 'stg.refresh_gl_accounts';
                    BEGIN PERFORM stg.refresh_gl_accounts(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'stg.refresh_journal_entries';
                    BEGIN PERFORM stg.refresh_journal_entries(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_gl_accounts';
                    BEGIN PERFORM mart.refresh_gl_accounts(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_gl_accounts_from_journal_lines';
                    BEGIN PERFORM mart.refresh_gl_accounts_from_journal_lines(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_account_balances';
                    BEGIN PERFORM mart.refresh_account_balances(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_income_statement';
                    BEGIN PERFORM mart.refresh_income_statement(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_balance_sheet';
                    BEGIN PERFORM mart.refresh_balance_sheet(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;

                    v_step := 'mart.refresh_ebitda';
                    BEGIN PERFORM mart.refresh_ebitda(p_company_id);
                        v_status := 'OK'; v_ts := NOW(); v_msg := NULL;
                    EXCEPTION WHEN OTHERS THEN v_status := 'ERROR'; v_ts := NOW(); v_msg := SQLERRM; END;
                    RETURN QUERY SELECT v_step, v_status, v_ts, v_msg;
                END;
                $func$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_gl_accounts_from_journal_lines(TEXT);");
        }
    }
}
