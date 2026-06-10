using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class AddCfgSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "cfg");

            // ── cfg.process ────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.process (
                    process_id      UUID        NOT NULL DEFAULT gen_random_uuid(),
                    process_code    TEXT        NOT NULL,
                    process_name    TEXT        NOT NULL,
                    description     TEXT,
                    display_order   INTEGER     NOT NULL DEFAULT 0,
                    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
                    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (process_id),
                    UNIQUE (process_code)
                );
                """);

            // ── cfg.dashboard ──────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.dashboard (
                    dashboard_id    UUID        NOT NULL DEFAULT gen_random_uuid(),
                    process_code    TEXT        NOT NULL,
                    dashboard_code  TEXT        NOT NULL,
                    dashboard_name  TEXT        NOT NULL,
                    dashboard_type  TEXT        NOT NULL,
                    description     TEXT,
                    route_hint      TEXT,
                    display_order   INTEGER     NOT NULL DEFAULT 0,
                    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
                    PRIMARY KEY (dashboard_id),
                    UNIQUE (process_code, dashboard_code)
                );
                """);

            // ── cfg.kpi ────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.kpi (
                    kpi_id          UUID        NOT NULL DEFAULT gen_random_uuid(),
                    process_code    TEXT        NOT NULL,
                    kpi_code        TEXT        NOT NULL,
                    kpi_name        TEXT        NOT NULL,
                    description     TEXT,
                    formula_text    TEXT        NOT NULL,
                    unit            TEXT,
                    format_type     TEXT        NOT NULL,
                    source_layer    TEXT        NOT NULL,
                    source_table    TEXT,
                    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
                    PRIMARY KEY (kpi_id),
                    UNIQUE (process_code, kpi_code)
                );
                """);

            // ── cfg.kpi_formula ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.kpi_formula (
                    formula_id          UUID        NOT NULL DEFAULT gen_random_uuid(),
                    kpi_code            TEXT        NOT NULL,
                    formula_sql         TEXT,
                    formula_human       TEXT        NOT NULL,
                    dependencies        TEXT[],
                    validation_rule     TEXT,
                    created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (formula_id)
                );
                """);

            // ── cfg.dashboard_widget ───────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.dashboard_widget (
                    widget_id           UUID        NOT NULL DEFAULT gen_random_uuid(),
                    dashboard_code      TEXT        NOT NULL,
                    kpi_code            TEXT,
                    widget_code         TEXT        NOT NULL,
                    widget_type         TEXT        NOT NULL,
                    title               TEXT        NOT NULL,
                    data_endpoint_hint  TEXT,
                    display_order       INTEGER     NOT NULL DEFAULT 0,
                    config_json         JSONB,
                    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
                    PRIMARY KEY (widget_id)
                );
                """);

            // ── cfg.sap_object_catalog ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.sap_object_catalog (
                    sap_object_code         TEXT        NOT NULL,
                    process_code            TEXT        NOT NULL,
                    sap_endpoint            TEXT        NOT NULL,
                    object_name             TEXT        NOT NULL,
                    object_type             TEXT        NOT NULL,
                    supports_incremental    BOOLEAN     NOT NULL DEFAULT TRUE,
                    incremental_field       TEXT,
                    default_page_size       INTEGER     NOT NULL DEFAULT 100,
                    is_active               BOOLEAN     NOT NULL DEFAULT FALSE,
                    PRIMARY KEY (sap_object_code)
                );
                """);

            // ── cfg.company_process_enabled (EF-managed) ──────────────────────────
            migrationBuilder.CreateTable(
                name: "company_process_enabled",
                schema: "cfg",
                columns: table => new
                {
                    company_id   = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    process_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    enabled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_process_enabled", x => new { x.company_id, x.process_code });
                });

            // ── SEED: cfg.process ──────────────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.process (process_code, process_name, description, display_order)
                VALUES
                    ('SALES',       'Ventas',       'Análisis de ventas, clientes y cumplimiento de pedidos', 1),
                    ('PURCHASING',  'Compras',       'Gestión de proveedores, órdenes de compra y recepciones', 2),
                    ('INVENTORY',   'Inventario',    'Control de stock, rotación y valorización de inventario', 3),
                    ('FINANCE',     'Finanzas',      'Cuentas por cobrar, cuentas por pagar y flujo de caja', 4),
                    ('OPERATIONS',  'Operaciones',   'Salud del pipeline de datos y monitoreo operativo', 5)
                ON CONFLICT (process_code) DO NOTHING;
                """);

            // ── SEED: cfg.dashboard ────────────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.dashboard (process_code, dashboard_code, dashboard_name, dashboard_type, description, route_hint, display_order)
                VALUES
                    ('SALES',      'SALES_EXECUTIVE',           'Resumen Ejecutivo',              'EXECUTIVE',   'KPIs principales de ventas del período',                '/client/bi/dashboard',  1),
                    ('SALES',      'SALES_CUSTOMERS',           'Análisis de Clientes',           'ANALYTICAL',  'Ranking de clientes por ventas netas',                   '/client/bi/sales',      2),
                    ('SALES',      'SALES_ITEMS_MARGIN',        'Productos y Margen',             'ANALYTICAL',  'Ventas por producto con margen estimado',                '/client/bi/sales',      3),
                    ('SALES',      'SALES_ORDER_FULFILLMENT',   'Cumplimiento de Pedidos',        'OPERATIONAL', 'Tasa de cumplimiento y pedidos pendientes',              NULL,                    4),

                    ('PURCHASING', 'PURCHASING_EXECUTIVE',      'Resumen Ejecutivo Compras',      'EXECUTIVE',   'KPIs principales de compras del período',                NULL, 1),
                    ('PURCHASING', 'PURCHASING_SUPPLIERS',      'Análisis de Proveedores',        'ANALYTICAL',  'Ranking de proveedores por monto comprado',             NULL, 2),
                    ('PURCHASING', 'PURCHASING_RECEIVING',      'Control de Recepciones',         'OPERATIONAL', 'Tasa de recepción de órdenes de compra',                NULL, 3),
                    ('PURCHASING', 'PURCHASING_PRICE_VARIATION','Variación de Precios',           'CONTROL',     'Análisis de variación de precios por proveedor/ítem',   NULL, 4),

                    ('INVENTORY',  'INVENTORY_EXECUTIVE',       'Resumen Ejecutivo Inventario',   'EXECUTIVE',   'KPIs principales de stock y valorización',              NULL, 1),
                    ('INVENTORY',  'INVENTORY_STOCK_VALUE',     'Valor de Inventario',            'ANALYTICAL',  'Stock disponible y valorización por almacén',           NULL, 2),
                    ('INVENTORY',  'INVENTORY_ROTATION_COVERAGE','Rotación y Cobertura',          'ANALYTICAL',  'Días de cobertura y clasificación FAST/SLOW/STOCKOUT',  NULL, 3),
                    ('INVENTORY',  'INVENTORY_WAREHOUSE_TRANSFERS','Transferencias entre Bodegas','OPERATIONAL', 'Movimientos de stock entre almacenes',                  NULL, 4),

                    ('FINANCE',    'FINANCE_EXECUTIVE',         'Resumen Ejecutivo Finanzas',     'EXECUTIVE',   'Posición AR/AP y riesgo de mora',                       NULL, 1),
                    ('FINANCE',    'FINANCE_AR_AGING',          'Aging Cuentas por Cobrar',       'CONTROL',     'Envejecimiento de cuentas por cobrar por cliente',      NULL, 2),
                    ('FINANCE',    'FINANCE_AP_AGING',          'Aging Cuentas por Pagar',        'CONTROL',     'Envejecimiento de cuentas por pagar por proveedor',     NULL, 3),
                    ('FINANCE',    'FINANCE_CASHFLOW_CONTROL',  'Control de Flujo de Caja',       'CONTROL',     'Proyección de cobros y pagos próximos',                 NULL, 4),

                    ('OPERATIONS', 'OPERATIONS_EXECUTIVE',      'Salud del Pipeline',             'EXECUTIVE',   'Estado general del pipeline SAP → DataBision',          '/client/bi/diagnostics', 1),
                    ('OPERATIONS', 'OPERATIONS_PIPELINE_HEALTH','Detalle del Pipeline',           'OPERATIONAL', 'Runs del extractor, transformaciones y checkpoints',    '/client/bi/diagnostics', 2),
                    ('OPERATIONS', 'OPERATIONS_DATA_QUALITY',   'Calidad de Datos',               'CONTROL',     'Issues detectados en el proceso de ETL',                NULL, 3),
                    ('OPERATIONS', 'OPERATIONS_ALERTS',         'Alertas Activas',                'CONTROL',     'Alertas abiertas por proceso y empresa',                NULL, 4)
                ON CONFLICT (process_code, dashboard_code) DO NOTHING;
                """);

            // ── SEED: cfg.kpi ──────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.kpi (process_code, kpi_code, kpi_name, description, formula_text, format_type, source_layer, source_table)
                VALUES
                    -- SALES
                    ('SALES', 'NET_SALES',        'Ventas Netas',          'Ventas brutas menos notas de crédito',                 'gross_sales_amount - credit_memo_amount',          'AMOUNT',   'MART', 'mart.sales_kpi_summary'),
                    ('SALES', 'GROSS_SALES',      'Ventas Brutas',         'Suma total de facturas emitidas',                      'SUM(doc_total) WHERE cancelled != Y',              'AMOUNT',   'MART', 'mart.sales_kpi_summary'),
                    ('SALES', 'INVOICE_COUNT',    'Facturas Emitidas',     'Cantidad de facturas activas del período',             'COUNT(OINV) WHERE cancelled != Y',                 'COUNT',    'MART', 'mart.sales_kpi_summary'),
                    ('SALES', 'ACTIVE_CUSTOMERS', 'Clientes Activos',      'Clientes con al menos una factura en el período',      'COUNT(DISTINCT card_code) WHERE cancelled != Y',   'COUNT',    'MART', 'mart.sales_kpi_summary'),
                    ('SALES', 'AVG_TICKET',       'Ticket Promedio',       'Monto promedio por factura',                          'gross_sales_amount / NULLIF(invoice_count, 0)',    'AMOUNT',   'MART', 'mart.sales_kpi_summary'),
                    ('SALES', 'FILL_RATE_PCT',    'Fill Rate',             'Tasa de cumplimiento de pedidos (requiere ORDR/ODLN)', 'delivered_qty / NULLIF(ordered_qty, 0)',           'PERCENT',  'MART', 'mart.sales_fulfillment_dashboard'),
                    ('SALES', 'GROSS_MARGIN_PCT', 'Margen Bruto %',        'Margen bruto estimado (requiere datos de costo)',      'gross_profit / NULLIF(net_sales_amount, 0)',       'PERCENT',  'MART', 'mart.sales_item_dashboard'),

                    -- PURCHASING
                    ('PURCHASING', 'PO_AMOUNT',          'Monto Órdenes de Compra', 'Suma de órdenes de compra del período (requiere OPOR)', 'SUM(doc_total) FROM stg.purchase_order',      'AMOUNT', 'MART', 'mart.purchase_executive_daily'),
                    ('PURCHASING', 'GOODS_RECEIPT_AMT',  'Monto Recepciones',       'Suma de recepciones de mercancía (requiere OPDN)',       'SUM(doc_total) FROM stg.purchase_delivery',   'AMOUNT', 'MART', 'mart.purchase_executive_daily'),
                    ('PURCHASING', 'ACTIVE_SUPPLIERS',   'Proveedores Activos',     'Proveedores con compras en el período',                  'COUNT(DISTINCT supplier_code)',               'COUNT',  'MART', 'mart.purchase_executive_daily'),
                    ('PURCHASING', 'PO_COUNT',           'Órdenes de Compra',       'Cantidad de órdenes de compra del período',              'COUNT(OPOR)',                                  'COUNT',  'MART', 'mart.purchase_executive_daily'),

                    -- INVENTORY
                    ('INVENTORY', 'STOCK_VALUE',       'Valor de Stock',       'Valorización total del inventario (requiere OITW)',       'SUM(on_hand * avg_price)',                     'AMOUNT',  'MART', 'mart.inventory_stock_dashboard'),
                    ('INVENTORY', 'STOCKOUT_ITEMS',    'Ítems sin Stock',      'Ítems con stock disponible = 0 o negativo',              'COUNT WHERE available_qty <= 0',               'COUNT',   'MART', 'mart.inventory_rotation_dashboard'),
                    ('INVENTORY', 'COVERAGE_DAYS',     'Días de Cobertura',    'Días de stock disponible según ventas promedio 30d',     'on_hand / NULLIF(avg_daily_sales_qty, 0)',     'DAYS',    'MART', 'mart.inventory_rotation_dashboard'),
                    ('INVENTORY', 'SLOW_MOVING_ITEMS', 'Ítems Movimiento Lento','Ítems con rotación SLOW o NO_MOVEMENT',               'COUNT WHERE rotation_status IN (SLOW,NO_MOVEMENT)', 'COUNT', 'MART', 'mart.inventory_rotation_dashboard'),

                    -- FINANCE
                    ('FINANCE', 'AR_TOTAL',         'Total CxC',            'Total de cuentas por cobrar vigentes y vencidas',        'SUM(balance_due) FROM mart.finance_ar_aging',  'AMOUNT',  'MART', 'mart.finance_executive_daily'),
                    ('FINANCE', 'AR_OVERDUE',       'CxC Vencida',          'Cuentas por cobrar con vencimiento pasado',              'SUM WHERE doc_due_date < CURRENT_DATE',        'AMOUNT',  'MART', 'mart.finance_ar_aging_dashboard'),
                    ('FINANCE', 'AR_OVERDUE_PCT',   'Mora CxC %',           'Porcentaje de la CxC que está vencida',                  'ar_overdue / NULLIF(ar_total, 0)',             'PERCENT', 'MART', 'mart.finance_executive_daily'),
                    ('FINANCE', 'AP_TOTAL',         'Total CxP',            'Total cuentas por pagar (requiere OPCH)',                 'SUM(balance_due) FROM mart.finance_ap_aging',  'AMOUNT',  'MART', 'mart.finance_executive_daily'),

                    -- OPERATIONS
                    ('OPERATIONS', 'PIPELINE_HEALTH',    'Estado Pipeline',    'Estado general del pipeline SAP → DataBision',         'ops.pipeline_health.health_status',            'TEXT',   'MART', 'ops.pipeline_health'),
                    ('OPERATIONS', 'DQ_ERRORS',          'Errores Calidad',    'Issues de calidad de datos sin resolver (ERROR+)',      'COUNT FROM ops.data_quality_issue WHERE severity IN (ERROR,CRITICAL) AND resolved_at_utc IS NULL', 'COUNT', 'MART', 'ops.data_quality_issue'),
                    ('OPERATIONS', 'LAST_EXTRACTOR_RUN', 'Último Run Extractor','Timestamp del último run del extractor',              'MAX(started_at_utc) FROM ops.extractor_run',   'TEXT',   'MART', 'ops.extractor_run')
                ON CONFLICT (process_code, kpi_code) DO NOTHING;
                """);

            // ── SEED: cfg.kpi_formula ──────────────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.kpi_formula (kpi_code, formula_human, formula_sql, dependencies)
                VALUES
                    ('NET_SALES',       'Ventas Netas = Ventas Brutas − Notas de Crédito',
                     'gross_sales_amount - credit_memo_amount',
                     ARRAY['mart.sales_kpi_summary']),

                    ('AVG_TICKET',      'Ticket Promedio = Ventas Brutas / Nro Facturas',
                     'gross_sales_amount / NULLIF(invoice_count, 0)',
                     ARRAY['mart.sales_kpi_summary']),

                    ('FILL_RATE_PCT',   'Fill Rate % = Unidades Entregadas / Unidades Pedidas',
                     'delivered_qty / NULLIF(ordered_qty, 0)',
                     ARRAY['mart.sales_fulfillment_dashboard', 'stg.order', 'stg.delivery']),

                    ('GROSS_MARGIN_PCT','Margen Bruto % = Ganancia Bruta / Ventas Netas',
                     'estimated_margin_amount / NULLIF(net_sales_amount, 0)',
                     ARRAY['mart.sales_item_dashboard']),

                    ('COVERAGE_DAYS',   'Días de Cobertura = Stock Disponible / Promedio Ventas Diarias 30d',
                     'on_hand / NULLIF(avg_daily_sales_qty, 0)',
                     ARRAY['mart.inventory_rotation_dashboard', 'stg.item_warehouse']),

                    ('AR_OVERDUE_PCT',  'Mora CxC % = CxC Vencida / Total CxC',
                     'overdue_amount / NULLIF(ar_total, 0)',
                     ARRAY['mart.finance_executive_daily', 'mart.finance_ar_aging_dashboard'])
                ON CONFLICT DO NOTHING;
                """);

            // ── SEED: cfg.sap_object_catalog ───────────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.sap_object_catalog
                    (sap_object_code, process_code, sap_endpoint, object_name, object_type, supports_incremental, incremental_field, default_page_size, is_active)
                VALUES
                    -- SALES (active objects — already extracted)
                    ('OINV',  'SALES',     'Invoices',                    'Facturas de Venta',            'HEADER',   TRUE,  'UpdateDate', 100, TRUE),
                    ('INV1',  'SALES',     'Invoices(DocEntry)/DocumentLines','Líneas de Factura de Venta','LINE',    FALSE, NULL,         100, TRUE),
                    ('ORIN',  'SALES',     'CreditNotes',                 'Notas de Crédito',             'HEADER',   TRUE,  'UpdateDate', 100, TRUE),
                    ('RIN1',  'SALES',     'CreditNotes(DocEntry)/DocumentLines','Líneas de Nota de Crédito','LINE', FALSE, NULL,         100, TRUE),
                    ('OCRD',  'SALES',     'BusinessPartners',            'Socios de Negocio (Clientes)',  'MASTER',   TRUE,  'UpdateDate', 100, TRUE),
                    ('OSLP',  'SALES',     'SalesPersons',                'Vendedores',                   'MASTER',   FALSE, NULL,         100, TRUE),
                    ('OITM',  'SALES',     'Items',                       'Maestro de Artículos',         'MASTER',   TRUE,  'UpdateDate', 100, TRUE),

                    -- SALES (prepared, not yet active)
                    ('ORDR',  'SALES',     'Orders',                      'Pedidos de Venta',             'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('RDR1',  'SALES',     'Orders(DocEntry)/DocumentLines','Líneas de Pedido de Venta',  'LINE',     FALSE, NULL,         100, FALSE),
                    ('ODLN',  'SALES',     'DeliveryNotes',               'Entregas de Venta',            'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('DLN1',  'SALES',     'DeliveryNotes(DocEntry)/DocumentLines','Líneas de Entrega',  'LINE',     FALSE, NULL,         100, FALSE),

                    -- PURCHASING (prepared)
                    ('OPOR',  'PURCHASING','PurchaseOrders',              'Órdenes de Compra',            'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('POR1',  'PURCHASING','PurchaseOrders(DocEntry)/DocumentLines','Líneas de OC',      'LINE',     FALSE, NULL,         100, FALSE),
                    ('OPDN',  'PURCHASING','PurchaseDeliveryNotes',       'Recepciones de Mercancía',     'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('PDN1',  'PURCHASING','PurchaseDeliveryNotes(DocEntry)/DocumentLines','Líneas Recepción','LINE', FALSE, NULL,        100, FALSE),
                    ('OPCH',  'PURCHASING','PurchaseInvoices',            'Facturas de Proveedor',        'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('PCH1',  'PURCHASING','PurchaseInvoices(DocEntry)/DocumentLines','Líneas Fact. Proveedor','LINE',FALSE,NULL,        100, FALSE),

                    -- INVENTORY (prepared)
                    ('OITW',  'INVENTORY', 'ItemWarehouseInfoCollection', 'Stock por Bodega',             'MASTER',   FALSE, NULL,         200, FALSE),
                    ('OWHS',  'INVENTORY', 'Warehouses',                  'Bodegas',                      'MASTER',   FALSE, NULL,         100, FALSE),
                    ('OWTR',  'INVENTORY', 'StockTransfers',              'Transferencias de Stock',      'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('WTR1',  'INVENTORY', 'StockTransfers(DocEntry)/StockTransferLines','Líneas Transferencia','LINE',FALSE,NULL,       100, FALSE),

                    -- FINANCE (prepared)
                    ('OJDT',  'FINANCE',   'JournalEntries',              'Asientos Contables',           'HEADER',   TRUE,  'ReferenceDate', 100, FALSE),
                    ('JDT1',  'FINANCE',   'JournalEntries(JdtNum)/Lines','Líneas de Asiento',            'LINE',     FALSE, NULL,         100, FALSE),
                    ('ORCT',  'FINANCE',   'IncomingPayments',            'Cobros Recibidos',             'HEADER',   TRUE,  'UpdateDate', 100, FALSE),
                    ('OVPM',  'FINANCE',   'VendorPayments',              'Pagos a Proveedores',          'HEADER',   TRUE,  'UpdateDate', 100, FALSE)
                ON CONFLICT (sap_object_code) DO NOTHING;
                """);

            // ── SEED: cfg.dashboard_widget (key widgets per dashboard) ─────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.dashboard_widget (dashboard_code, kpi_code, widget_code, widget_type, title, data_endpoint_hint, display_order)
                VALUES
                    -- SALES_EXECUTIVE
                    ('SALES_EXECUTIVE', 'NET_SALES',        'SALES_EXEC_NET_SALES',      'KPI_CARD',  'Ventas Netas',        '/api/client/bi/dashboard/summary', 1),
                    ('SALES_EXECUTIVE', 'INVOICE_COUNT',    'SALES_EXEC_INV_COUNT',      'KPI_CARD',  'Facturas',            '/api/client/bi/dashboard/summary', 2),
                    ('SALES_EXECUTIVE', 'ACTIVE_CUSTOMERS', 'SALES_EXEC_CUSTOMERS',      'KPI_CARD',  'Clientes Activos',    '/api/client/bi/dashboard/summary', 3),
                    ('SALES_EXECUTIVE', 'AVG_TICKET',       'SALES_EXEC_AVG_TICKET',     'KPI_CARD',  'Ticket Promedio',     '/api/client/bi/dashboard/summary', 4),
                    ('SALES_EXECUTIVE', NULL,               'SALES_EXEC_CHART',          'BAR_CHART', 'Ventas Diarias 30d',  '/api/client/bi/dashboard/sales-daily', 5),
                    ('SALES_EXECUTIVE', NULL,               'SALES_EXEC_TOP_CUST',       'TABLE',     'Top Clientes',        '/api/client/bi/dashboard/top-customers', 6),

                    -- SALES_CUSTOMERS
                    ('SALES_CUSTOMERS', NULL, 'SALES_CUST_TABLE',   'TABLE', 'Clientes por Ventas',   '/api/client/bi/sales/customers',   1),

                    -- SALES_ITEMS_MARGIN
                    ('SALES_ITEMS_MARGIN', NULL, 'SALES_ITEMS_TABLE', 'TABLE', 'Productos por Ventas', '/api/client/bi/sales/items', 1),

                    -- FINANCE_AR_AGING
                    ('FINANCE_AR_AGING', 'AR_TOTAL',    'FIN_AR_TOTAL',      'KPI_CARD',  'Total CxC',           '/api/client/bi/finance/ar-aging', 1),
                    ('FINANCE_AR_AGING', 'AR_OVERDUE',  'FIN_AR_OVERDUE',    'KPI_CARD',  'CxC Vencida',         '/api/client/bi/finance/ar-aging', 2),
                    ('FINANCE_AR_AGING', 'AR_OVERDUE_PCT','FIN_AR_MORA',     'KPI_CARD',  'Mora %',              '/api/client/bi/finance/ar-aging', 3),
                    ('FINANCE_AR_AGING', NULL,          'FIN_AR_TABLE',      'TABLE',     'Detalle CxC Aging',   '/api/client/bi/finance/ar-aging', 4),

                    -- OPERATIONS_EXECUTIVE
                    ('OPERATIONS_EXECUTIVE', 'PIPELINE_HEALTH', 'OPS_HEALTH_BADGE', 'STATUS_BADGE', 'Estado Pipeline', '/api/client/bi/diagnostics', 1),
                    ('OPERATIONS_EXECUTIVE', NULL, 'OPS_CHECKS_TABLE', 'TABLE', 'Verificaciones del Sistema', '/api/client/bi/diagnostics', 2)
                ON CONFLICT DO NOTHING;
                """);

            // ── SEED: cfg.company_process_enabled (company-dev-001) ────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg.company_process_enabled (company_id, process_code, is_enabled, enabled_at_utc)
                VALUES
                    ('company-dev-001', 'SALES',      TRUE, NOW()),
                    ('company-dev-001', 'PURCHASING',  TRUE, NOW()),
                    ('company-dev-001', 'INVENTORY',   TRUE, NOW()),
                    ('company-dev-001', 'FINANCE',     TRUE, NOW()),
                    ('company-dev-001', 'OPERATIONS',  TRUE, NOW())
                ON CONFLICT (company_id, process_code) DO NOTHING;

                -- KSDEPOR: no seeding here — company_id not confirmed in codebase.
                -- Run manually: INSERT INTO cfg.company_process_enabled VALUES ('ksdepor-company-id', 'SALES', TRUE, NOW());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "company_process_enabled", schema: "cfg");

            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.dashboard_widget;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.kpi_formula;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.kpi;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.sap_object_catalog;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS cfg.process;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS cfg;");
        }
    }
}
