using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint Purchase MART: Creates the 4 purchase analytics tables in the mart schema.
    ///
    /// Tables created:
    ///   mart.purchase_period_kpi    — aggregated KPIs per year/month (AP invoices + AP credit memos)
    ///   mart.top_suppliers          — supplier ranking by net purchases (12-month rolling window) + DPO
    ///   mart.top_purchase_items     — item ranking by purchase amount via PCH1 join, 12-month window
    ///   mart.open_purchase_orders   — open purchase orders pipeline (OPOR with doc_status = 'O')
    ///
    /// Source objects: raw.sap_opch, raw.sap_pch1, raw.sap_orpc, raw.sap_opor, raw.sap_por1,
    ///                 raw.sap_ocrd, raw.sap_oitm
    ///
    /// Applied manually via: dotnet ef database update --context StagingDbContext (port 5432)
    /// See: docs/operations/staging-migrations-runbook.md
    /// </summary>
    public partial class AddPurchaseMartTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.purchase_period_kpi (
    company_id          TEXT          NOT NULL,
    period_year         INT           NOT NULL,
    period_month        INT           NOT NULL,
    gross_purchases     NUMERIC(18,2) DEFAULT 0,
    credit_memo_amount  NUMERIC(18,2) DEFAULT 0,
    net_purchases       NUMERIC(18,2) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    credit_memo_count   INT           DEFAULT 0,
    active_suppliers    INT           DEFAULT 0,
    avg_ticket          NUMERIC(18,2) DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, period_year, period_month)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.top_suppliers (
    company_id          TEXT          NOT NULL,
    card_code           TEXT          NOT NULL,
    card_name           TEXT,
    gross_purchases     NUMERIC(18,2) DEFAULT 0,
    credit_memo_amount  NUMERIC(18,2) DEFAULT 0,
    net_purchases       NUMERIC(18,2) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    last_invoice_date   DATE,
    dpo_days            NUMERIC(8,2),
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, card_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.top_purchase_items (
    company_id          TEXT          NOT NULL,
    item_code           TEXT          NOT NULL,
    item_name           TEXT,
    item_group_name     TEXT,
    gross_purchases     NUMERIC(18,2) DEFAULT 0,
    quantity_purchased  NUMERIC(18,4) DEFAULT 0,
    invoice_count       INT           DEFAULT 0,
    avg_unit_price      NUMERIC(18,4) DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, item_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.open_purchase_orders (
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
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, doc_num)
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_purchase_period_kpi_company
    ON mart.purchase_period_kpi (company_id, period_year DESC, period_month DESC);

CREATE INDEX IF NOT EXISTS idx_top_suppliers_company_netpurchases
    ON mart.top_suppliers (company_id, net_purchases DESC);

CREATE INDEX IF NOT EXISTS idx_top_purchase_items_company_purchases
    ON mart.top_purchase_items (company_id, gross_purchases DESC);

CREATE INDEX IF NOT EXISTS idx_open_purchase_orders_company
    ON mart.open_purchase_orders (company_id, is_overdue DESC, doc_due_date ASC);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.open_purchase_orders;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.top_purchase_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.top_suppliers;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.purchase_period_kpi;");
        }
    }
}
