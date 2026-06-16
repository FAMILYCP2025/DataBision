using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Corrective migration: stg.refresh_all was broken in 20260615210000 because existing
    /// refresh functions (salesperson, customer, etc.) return INT (scalar), not TABLE(TEXT, INT).
    /// They must be called as scalar values, not with SELECT *.
    /// </summary>
    public partial class FixStgRefreshAll : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_all(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                BEGIN
                    RETURN QUERY SELECT 'salesperson'::TEXT,        stg.refresh_salesperson(p_company_id);
                    RETURN QUERY SELECT 'customer'::TEXT,           stg.refresh_customer(p_company_id);
                    RETURN QUERY SELECT 'item'::TEXT,               stg.refresh_item(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice'::TEXT,      stg.refresh_sales_invoice(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice_line'::TEXT, stg.refresh_sales_invoice_line(p_company_id);
                    RETURN QUERY SELECT 'credit_memo'::TEXT,        stg.refresh_credit_memo(p_company_id);
                    RETURN QUERY SELECT 'credit_memo_line'::TEXT,   stg.refresh_credit_memo_line(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_order(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_receipt(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_invoice(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_item_warehouse(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_sales_order(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_delivery(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_stock_transfer(p_company_id);
                END;
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_all(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                BEGIN
                    RETURN QUERY SELECT 'salesperson'::TEXT,        stg.refresh_salesperson(p_company_id);
                    RETURN QUERY SELECT 'customer'::TEXT,           stg.refresh_customer(p_company_id);
                    RETURN QUERY SELECT 'item'::TEXT,               stg.refresh_item(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice'::TEXT,      stg.refresh_sales_invoice(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice_line'::TEXT, stg.refresh_sales_invoice_line(p_company_id);
                    RETURN QUERY SELECT 'credit_memo'::TEXT,        stg.refresh_credit_memo(p_company_id);
                    RETURN QUERY SELECT 'credit_memo_line'::TEXT,   stg.refresh_credit_memo_line(p_company_id);
                END;
                $$;
                """);
        }
    }
}
