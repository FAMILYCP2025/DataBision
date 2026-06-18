-- ============================================================
-- DataBision — Demo Account Classification Rules
-- Company: KSDEPOR / demo (AnalyticsCompanyId: company-dev-001)
-- Sprint 15D — 2026-06-18
--
-- ⚠️  TEMPLATE RULES — NOT universal truth.
--     These are reasonable defaults for a Chilean SAP B1 company
--     using format-code prefixes common in the Chilean IFRS chart.
--     MUST be reviewed and adjusted by the client accountant before
--     using MART data for financial reporting.
--
-- Usage: Run in Supabase SQL Editor against the analytics DB.
-- Idempotent: ON CONFLICT DO UPDATE — safe to re-run.
-- ============================================================

-- Verify target company_id exists before inserting
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "raw"."sap_oact" WHERE company_id = 'company-dev-001' LIMIT 1
    ) THEN
        RAISE NOTICE 'WARNING: No OACT data found for company-dev-001. Extract OACT first, then run classification rules.';
    END IF;
END;
$$;

-- ── Remove stale template rules (idempotent cleanup) ─────────────────────────
-- Only removes format_code prefix rules (account_code = NULL).
-- Specific account-code rules added manually are preserved.
DELETE FROM cfg.account_classification_rules
WHERE company_id = 'company-dev-001'
  AND account_code IS NULL
  AND format_code IN (
    '1','11','12','13','14','15','16','17','18','19',
    '2','21','22','23','24','25','26','27','28','29',
    '3','31','32','33','34','35',
    '4','41','42','43','44','45','46','47','48','49',
    '5','51','52','53','54','55',
    '6','61','62','63','64','65','66','67','68','69',
    '7','71','72','73','74','75','76','77','78','79',
    '8','81','82','83','84','85','86','87','88','89'
  );

-- ── Balance Sheet: Assets (Activos) ──────────────────────────────────────────
-- Level-1 format_code 1 = Assets hierarchy root (catch-all for unmapped sub-ranges)

INSERT INTO cfg.account_classification_rules (company_id, account_code, format_code, statement_line, created_at, updated_at)
VALUES
    -- Current Assets (Activo Corriente)
    ('company-dev-001', NULL, '11', 'current_assets',     NOW(), NOW()),  -- Caja y equivalentes / Efectivo
    ('company-dev-001', NULL, '12', 'current_assets',     NOW(), NOW()),  -- CxC / Clientes
    ('company-dev-001', NULL, '13', 'current_assets',     NOW(), NOW()),  -- Inventario / Existencias
    ('company-dev-001', NULL, '14', 'current_assets',     NOW(), NOW()),  -- Otros activos corrientes

    -- Non-Current Assets (Activo No Corriente)
    ('company-dev-001', NULL, '15', 'non_current_assets', NOW(), NOW()),  -- Activo fijo / PPE
    ('company-dev-001', NULL, '16', 'non_current_assets', NOW(), NOW()),  -- Activos intangibles
    ('company-dev-001', NULL, '17', 'non_current_assets', NOW(), NOW()),  -- Inversiones LP
    ('company-dev-001', NULL, '18', 'non_current_assets', NOW(), NOW()),  -- Otros activos no corrientes

    -- ── Balance Sheet: Liabilities (Pasivos) ─────────────────────────────────
    -- Current Liabilities (Pasivo Corriente)
    ('company-dev-001', NULL, '21', 'current_liabilities',      NOW(), NOW()),  -- CxP / Proveedores
    ('company-dev-001', NULL, '22', 'current_liabilities',      NOW(), NOW()),  -- Deudas bancarias CP
    ('company-dev-001', NULL, '23', 'current_liabilities',      NOW(), NOW()),  -- Remuneraciones por pagar
    ('company-dev-001', NULL, '24', 'current_liabilities',      NOW(), NOW()),  -- IVA / Impuestos por pagar
    ('company-dev-001', NULL, '25', 'current_liabilities',      NOW(), NOW()),  -- Otros pasivos corrientes

    -- Non-Current Liabilities (Pasivo No Corriente)
    ('company-dev-001', NULL, '26', 'non_current_liabilities',  NOW(), NOW()),  -- Deudas LP
    ('company-dev-001', NULL, '27', 'non_current_liabilities',  NOW(), NOW()),  -- Provisiones LP
    ('company-dev-001', NULL, '28', 'non_current_liabilities',  NOW(), NOW()),  -- Otros pasivos no corrientes

    -- ── Balance Sheet: Equity (Patrimonio) ───────────────────────────────────
    ('company-dev-001', NULL, '31', 'equity', NOW(), NOW()),  -- Capital pagado
    ('company-dev-001', NULL, '32', 'equity', NOW(), NOW()),  -- Reservas
    ('company-dev-001', NULL, '33', 'equity', NOW(), NOW()),  -- Utilidades retenidas / Resultado acumulado
    ('company-dev-001', NULL, '34', 'equity', NOW(), NOW()),  -- Resultado del ejercicio

    -- ── P&L: Revenue (Ingresos / Ventas) ─────────────────────────────────────
    ('company-dev-001', NULL, '41', 'revenue', NOW(), NOW()),  -- Ventas netas
    ('company-dev-001', NULL, '42', 'revenue', NOW(), NOW()),  -- Descuentos y devoluciones (puede necesitar negación manual)
    ('company-dev-001', NULL, '43', 'revenue', NOW(), NOW()),  -- Otros ingresos operacionales
    ('company-dev-001', NULL, '4',  'revenue', NOW(), NOW()),  -- Fallback: toda la familia 4xxx = revenue

    -- ── P&L: Cost of Goods Sold (Costo de Ventas) ────────────────────────────
    ('company-dev-001', NULL, '51', 'cogs', NOW(), NOW()),  -- Costo de mercadería vendida
    ('company-dev-001', NULL, '52', 'cogs', NOW(), NOW()),  -- Variación de existencias
    ('company-dev-001', NULL, '5',  'cogs', NOW(), NOW()),  -- Fallback: toda la familia 5xxx = cogs

    -- ── P&L: Operating Expenses (Gastos Operacionales) ───────────────────────
    ('company-dev-001', NULL, '61', 'opex', NOW(), NOW()),  -- Remuneraciones y beneficios
    ('company-dev-001', NULL, '62', 'opex', NOW(), NOW()),  -- Arriendos y gastos de ocupación
    ('company-dev-001', NULL, '63', 'opex', NOW(), NOW()),  -- Gastos de marketing y publicidad
    ('company-dev-001', NULL, '64', 'opex', NOW(), NOW()),  -- Gastos de administración general
    ('company-dev-001', NULL, '65', 'opex', NOW(), NOW()),  -- Servicios externos / honorarios
    ('company-dev-001', NULL, '66', 'opex', NOW(), NOW()),  -- Gastos de tecnología y sistemas
    ('company-dev-001', NULL, '67', 'depreciation', NOW(), NOW()),  -- Depreciación y amortización
    ('company-dev-001', NULL, '68', 'opex', NOW(), NOW()),  -- Otros gastos operacionales
    ('company-dev-001', NULL, '6',  'opex', NOW(), NOW()),  -- Fallback: toda la familia 6xxx = opex

    -- ── P&L: Other Income / Expense ──────────────────────────────────────────
    ('company-dev-001', NULL, '71', 'other_income',   NOW(), NOW()),  -- Otros ingresos no operacionales
    ('company-dev-001', NULL, '72', 'other_expense',  NOW(), NOW()),  -- Otros gastos no operacionales
    ('company-dev-001', NULL, '7',  'other_income',   NOW(), NOW()),  -- Fallback: familia 7xxx = other_income

    -- ── P&L: Financial ───────────────────────────────────────────────────────
    ('company-dev-001', NULL, '81', 'financial', NOW(), NOW()),  -- Ingresos financieros (intereses ganados)
    ('company-dev-001', NULL, '82', 'financial', NOW(), NOW()),  -- Gastos financieros (intereses pagados)
    ('company-dev-001', NULL, '83', 'financial', NOW(), NOW()),  -- Diferencias de cambio
    ('company-dev-001', NULL, '8',  'financial', NOW(), NOW()),  -- Fallback: familia 8xxx = financial

    -- ── P&L: Tax ─────────────────────────────────────────────────────────────
    ('company-dev-001', NULL, '91', 'tax', NOW(), NOW()),  -- Impuesto a la renta (ISR / impuesto corporativo)
    ('company-dev-001', NULL, '9',  'tax', NOW(), NOW())   -- Fallback: familia 9xxx = tax

ON CONFLICT (company_id, account_code, format_code) DO UPDATE
    SET statement_line = EXCLUDED.statement_line,
        updated_at     = NOW();

-- ── Verify inserted rules ─────────────────────────────────────────────────────
SELECT statement_line, COUNT(*) AS rules
FROM cfg.account_classification_rules
WHERE company_id = 'company-dev-001'
GROUP BY statement_line
ORDER BY statement_line;
