using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    public partial class AddFinanceMartTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.finance_summary (
    company_id          TEXT          NOT NULL,
    total_open_ar       NUMERIC(18,2) DEFAULT 0,
    total_overdue_ar    NUMERIC(18,2) DEFAULT 0,
    ar_customer_count   INT           DEFAULT 0,
    dso_days            NUMERIC(8,2),
    total_open_ap       NUMERIC(18,2) DEFAULT 0,
    total_overdue_ap    NUMERIC(18,2) DEFAULT 0,
    ap_supplier_count   INT           DEFAULT 0,
    dpo_days            NUMERIC(8,2),
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.ar_aging (
    company_id      TEXT          NOT NULL,
    card_code       TEXT          NOT NULL,
    card_name       TEXT,
    current_amount  NUMERIC(18,2) DEFAULT 0,
    bucket_1_30     NUMERIC(18,2) DEFAULT 0,
    bucket_31_60    NUMERIC(18,2) DEFAULT 0,
    bucket_61_90    NUMERIC(18,2) DEFAULT 0,
    bucket_91_120   NUMERIC(18,2) DEFAULT 0,
    bucket_over_120 NUMERIC(18,2) DEFAULT 0,
    total_open      NUMERIC(18,2) DEFAULT 0,
    invoice_count   INT           DEFAULT 0,
    oldest_due_date DATE,
    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, card_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.ap_aging (
    company_id      TEXT          NOT NULL,
    card_code       TEXT          NOT NULL,
    card_name       TEXT,
    current_amount  NUMERIC(18,2) DEFAULT 0,
    bucket_1_30     NUMERIC(18,2) DEFAULT 0,
    bucket_31_60    NUMERIC(18,2) DEFAULT 0,
    bucket_61_90    NUMERIC(18,2) DEFAULT 0,
    bucket_91_120   NUMERIC(18,2) DEFAULT 0,
    bucket_over_120 NUMERIC(18,2) DEFAULT 0,
    total_open      NUMERIC(18,2) DEFAULT 0,
    invoice_count   INT           DEFAULT 0,
    oldest_due_date DATE,
    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, card_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.finance_period_kpi (
    company_id        TEXT          NOT NULL,
    period_year       INT           NOT NULL,
    period_month      INT           NOT NULL,
    ar_billed         NUMERIC(18,2) DEFAULT 0,
    ar_credit_memo    NUMERIC(18,2) DEFAULT 0,
    ar_net            NUMERIC(18,2) DEFAULT 0,
    ar_invoice_count  INT           DEFAULT 0,
    ap_billed         NUMERIC(18,2) DEFAULT 0,
    ap_credit_memo    NUMERIC(18,2) DEFAULT 0,
    ap_net            NUMERIC(18,2) DEFAULT 0,
    ap_invoice_count  INT           DEFAULT 0,
    refreshed_at      TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, period_year, period_month)
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_finance_summary_company
    ON mart.finance_summary (company_id);

CREATE INDEX IF NOT EXISTS idx_ar_aging_company_total
    ON mart.ar_aging (company_id, total_open DESC);

CREATE INDEX IF NOT EXISTS idx_ap_aging_company_total
    ON mart.ap_aging (company_id, total_open DESC);

CREATE INDEX IF NOT EXISTS idx_finance_period_kpi_company
    ON mart.finance_period_kpi (company_id, period_year DESC, period_month DESC);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.finance_period_kpi;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.ap_aging;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.ar_aging;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.finance_summary;");
        }
    }
}
