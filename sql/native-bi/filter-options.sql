-- Native BI — Filter Options Queries
-- Source tables in mart.* schema (Supabase/PostgreSQL)
-- Used by: GET /api/client/bi/filters/{type}
-- All queries filtered by company_id. Resilient — return empty if table/column missing.

-- Item groups (from mart.sales_item_dashboard)
SELECT DISTINCT
    item_group_code AS "Code",
    COALESCE(item_group_name, item_group_code) AS "Name"
FROM mart.sales_item_dashboard
WHERE company_id = :company_id
  AND item_group_code IS NOT NULL
  AND item_group_code <> ''
ORDER BY "Name";

-- Customer groups (from mart.dim_customers)
SELECT DISTINCT
    customer_group_code AS "Code",
    COALESCE(customer_group_name, customer_group_code) AS "Name"
FROM mart.dim_customers
WHERE company_id = :company_id
  AND customer_group_code IS NOT NULL
  AND customer_group_code <> ''
ORDER BY "Name";

-- Supplier groups (from mart.dim_suppliers)
SELECT DISTINCT
    supplier_group_code AS "Code",
    COALESCE(supplier_group_name, supplier_group_code) AS "Name"
FROM mart.dim_suppliers
WHERE company_id = :company_id
  AND supplier_group_code IS NOT NULL
  AND supplier_group_code <> ''
ORDER BY "Name";

-- Warehouses (from mart.inventory_warehouse)
SELECT DISTINCT
    warehouse_code AS "Code",
    COALESCE(warehouse_name, warehouse_code) AS "Name"
FROM mart.inventory_warehouse
WHERE company_id = :company_id
  AND warehouse_code IS NOT NULL
  AND warehouse_code <> ''
ORDER BY "Name";

-- Salespersons (from mart.sales_customer_dashboard)
-- Uses salesperson_name as Code until a dedicated dim_salespersons table is available
SELECT DISTINCT
    COALESCE(salesperson_code, salesperson_name) AS "Code",
    salesperson_name AS "Name"
FROM mart.sales_customer_dashboard
WHERE company_id = :company_id
  AND salesperson_name IS NOT NULL
  AND salesperson_name <> ''
ORDER BY "Name";
