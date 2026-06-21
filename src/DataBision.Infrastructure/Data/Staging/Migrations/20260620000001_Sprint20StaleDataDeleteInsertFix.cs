using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint 20A/B: Eliminates stale unclassified rows from MART summary tables.
    ///
    /// Root cause: refresh_income_statement, refresh_balance_sheet, and refresh_ebitda
    /// used INSERT ON CONFLICT DO UPDATE without first deleting obsolete rows. When accounts
    /// previously classified as 'unclassified' were reclassified (Sprint 19), the old
    /// 'unclassified' rows in the summary tables remained because INSERT never fires for
    /// combinations that no longer have data.
    ///
    /// Fix: prepend DELETE FROM [table] WHERE company_id = p_company_id to each function
    /// before the INSERT, ensuring each refresh produces a clean, current snapshot.
    ///
    /// Affected functions:
    ///   mart.refresh_income_statement — adds DELETE to remove stale statement_line rows
    ///   mart.refresh_balance_sheet    — adds DELETE to remove stale category rows
    ///   mart.refresh_ebitda           — adds DELETE to remove stale period rows
    ///
    /// All functions remain CREATE OR REPLACE and idempotent.
    /// </summary>
    public partial class Sprint20StaleDataDeleteInsertFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_income_statement(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    DELETE FROM mart.income_statement_summary WHERE company_id = p_company_id;
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
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_balance_sheet(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    DELETE FROM mart.balance_sheet_summary WHERE company_id = p_company_id;
    INSERT INTO mart.balance_sheet_summary (
        company_id, snapshot_date, category, sub_category, amount, refreshed_at
    )
    SELECT
        ab.company_id,
        (DATE_TRUNC('month', MAKE_DATE(ab.period_year, ab.period_month, 1))
            + INTERVAL '1 month' - INTERVAL '1 day')::DATE AS snapshot_date,
        COALESCE(ga.statement_line, 'unclassified') AS category,
        COALESCE(ga.account_type, '')               AS sub_category,
        CASE COALESCE(ga.statement_line, 'unclassified')
            WHEN 'current_assets'          THEN SUM(ab.debit_sum  - ab.credit_sum)
            WHEN 'non_current_assets'      THEN SUM(ab.debit_sum  - ab.credit_sum)
            WHEN 'current_liabilities'     THEN SUM(ab.credit_sum - ab.debit_sum)
            WHEN 'non_current_liabilities' THEN SUM(ab.credit_sum - ab.debit_sum)
            WHEN 'equity'                  THEN SUM(ab.credit_sum - ab.debit_sum)
            ELSE                                SUM(ab.debit_sum  - ab.credit_sum)
        END AS amount,
        NOW()
    FROM mart.account_balances ab
    LEFT JOIN mart.gl_accounts ga ON ga.company_id = ab.company_id AND ga.code = ab.code
    WHERE ab.company_id = p_company_id
      AND COALESCE(ga.statement_line, 'unclassified') IN (
          'current_assets','non_current_assets',
          'current_liabilities','non_current_liabilities','equity','unclassified')
    GROUP BY ab.company_id, ab.period_year, ab.period_month,
             COALESCE(ga.statement_line, 'unclassified'), COALESCE(ga.account_type, '')
    ON CONFLICT (company_id, snapshot_date, category, sub_category) DO UPDATE SET
        amount = EXCLUDED.amount, refreshed_at = NOW();
END;
$func$;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_ebitda(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    DELETE FROM mart.ebitda_summary WHERE company_id = p_company_id;
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
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback for function updates — Sprint 19 versions remain in prior migration.
        }
    }
}
