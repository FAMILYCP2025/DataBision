using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint Sales MART: Creates the 5 sales analytics tables in the mart schema.
    ///
    /// Tables created:
    ///   mart.sales_period_kpi      — aggregated KPIs per year/month (gross sales, net sales, avg ticket, return rate)
    ///   mart.top_customers         — customer ranking by net sales (12-month rolling window) + DSO
    ///   mart.top_items             — item ranking by net sales (12-month rolling window)
    ///   mart.top_salespersons      — salesperson ranking by net sales (12-month rolling window)
    ///   mart.open_sales_orders     — open sales orders pipeline (ORDR with doc_status = 'O')
    ///
    /// Source objects: raw.sap_oinv, raw.sap_orin, raw.sap_inv1, raw.sap_rin1,
    ///                 raw.sap_ocrd, raw.sap_oitm, raw.sap_oslp, raw.sap_ordr
    ///
    /// Applied manually via: dotnet ef database update --context StagingDbContext (port 5432)
    /// See: docs/operations/staging-migrations-runbook.md
    /// </summary>
    public partial class AddSalesMartTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.sales_period_kpi (
    company_id          TEXT          NOT NULL,
    period_year         INT           NOT NULL,
    period_month        INT           NOT NULL,
    gross_sales         NUMERIC(18,2) DEFAULT 0,
    credit_memo_amount  NUMERIC(18,2) DEFAULT 0,
    net_sales           NUMERIC(18,2) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    credit_memo_count   INT           DEFAULT 0,
    active_customers    INT           DEFAULT 0,
    avg_ticket          NUMERIC(18,2) DEFAULT 0,
    return_rate_pct     NUMERIC(8,4)  DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, period_year, period_month)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.top_customers (
    company_id          TEXT          NOT NULL,
    card_code           TEXT          NOT NULL,
    card_name           TEXT,
    gross_sales         NUMERIC(18,2) DEFAULT 0,
    credit_memo_amount  NUMERIC(18,2) DEFAULT 0,
    net_sales           NUMERIC(18,2) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    last_invoice_date   DATE,
    dso_days            NUMERIC(8,2),
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, card_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.top_items (
    company_id          TEXT          NOT NULL,
    item_code           TEXT          NOT NULL,
    item_name           TEXT,
    item_group_name     TEXT,
    gross_sales         NUMERIC(18,2) DEFAULT 0,
    credit_memo_amount  NUMERIC(18,2) DEFAULT 0,
    net_sales           NUMERIC(18,2) DEFAULT 0,
    quantity_sold       NUMERIC(18,4) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    avg_unit_price      NUMERIC(18,4) DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, item_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.top_salespersons (
    company_id          TEXT          NOT NULL,
    sales_person_code   INT           NOT NULL,
    sales_person_name   TEXT,
    net_sales           NUMERIC(18,2) DEFAULT 0,
    gross_sales         NUMERIC(18,2) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    active_customers    INT           DEFAULT 0,
    avg_ticket          NUMERIC(18,2) DEFAULT 0,
    return_rate_pct     NUMERIC(8,4)  DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, sales_person_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.open_sales_orders (
    company_id          TEXT          NOT NULL,
    doc_num             INT           NOT NULL,
    card_code           TEXT,
    card_name           TEXT,
    doc_date            DATE,
    doc_due_date        DATE,
    doc_total           NUMERIC(18,2) DEFAULT 0,
    open_amount         NUMERIC(18,2) DEFAULT 0,
    days_open           INT,
    is_overdue          BOOLEAN       DEFAULT FALSE,
    sales_person_name   TEXT,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, doc_num)
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_sales_period_kpi_company
    ON mart.sales_period_kpi (company_id, period_year DESC, period_month DESC);

CREATE INDEX IF NOT EXISTS idx_top_customers_company_netsales
    ON mart.top_customers (company_id, net_sales DESC);

CREATE INDEX IF NOT EXISTS idx_top_items_company_netsales
    ON mart.top_items (company_id, net_sales DESC);

CREATE INDEX IF NOT EXISTS idx_open_sales_orders_company
    ON mart.open_sales_orders (company_id, is_overdue DESC, doc_due_date ASC);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.open_sales_orders;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.top_salespersons;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.top_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.top_customers;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_period_kpi;");
        }
    }
}
