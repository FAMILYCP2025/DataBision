using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    public partial class AddInventoryMartTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.inventory_snapshot (
    company_id          TEXT          NOT NULL,
    item_code           TEXT          NOT NULL,
    item_name           TEXT,
    item_group_name     TEXT,
    on_hand             NUMERIC(18,4) DEFAULT 0,
    committed           NUMERIC(18,4) DEFAULT 0,
    ordered             NUMERIC(18,4) DEFAULT 0,
    available           NUMERIC(18,4) DEFAULT 0,
    avg_price           NUMERIC(18,4) DEFAULT 0,
    stock_value         NUMERIC(18,2) DEFAULT 0,
    last_purchase_date  DATE,
    last_sale_date      DATE,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, item_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.inventory_movement_kpi (
    company_id          TEXT          NOT NULL,
    period_year         INT           NOT NULL,
    period_month        INT           NOT NULL,
    inbound_qty         NUMERIC(18,4) DEFAULT 0,
    outbound_qty        NUMERIC(18,4) DEFAULT 0,
    net_qty             NUMERIC(18,4) DEFAULT 0,
    inbound_value       NUMERIC(18,2) DEFAULT 0,
    outbound_value      NUMERIC(18,2) DEFAULT 0,
    transaction_count   INT           DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, period_year, period_month)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.slow_moving_items (
    company_id            TEXT          NOT NULL,
    item_code             TEXT          NOT NULL,
    item_name             TEXT,
    item_group_name       TEXT,
    on_hand               NUMERIC(18,4) DEFAULT 0,
    stock_value           NUMERIC(18,2) DEFAULT 0,
    last_movement_date    DATE,
    days_without_movement INT,
    refreshed_at          TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, item_code)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mart.warehouse_stock (
    company_id          TEXT          NOT NULL,
    warehouse_code      TEXT          NOT NULL,
    warehouse_name      TEXT,
    total_items         INT           DEFAULT 0,
    total_on_hand       NUMERIC(18,4) DEFAULT 0,
    total_stock_value   NUMERIC(18,2) DEFAULT 0,
    items_below_min     INT           DEFAULT 0,
    refreshed_at        TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, warehouse_code)
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_inventory_snapshot_company
    ON mart.inventory_snapshot (company_id, stock_value DESC);

CREATE INDEX IF NOT EXISTS idx_inventory_movement_kpi_company
    ON mart.inventory_movement_kpi (company_id, period_year DESC, period_month DESC);

CREATE INDEX IF NOT EXISTS idx_slow_moving_items_company
    ON mart.slow_moving_items (company_id, days_without_movement DESC);

CREATE INDEX IF NOT EXISTS idx_warehouse_stock_company
    ON mart.warehouse_stock (company_id, total_stock_value DESC);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.warehouse_stock;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.slow_moving_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.inventory_movement_kpi;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.inventory_snapshot;");
        }
    }
}
