-- ============================================================
-- DataBision — Native BI Accounting MART ETL Functions
-- Sprint 13B — Generated 2026-06-17
-- ============================================================
-- Usage:  SELECT * FROM mart.refresh_accounting_all('company-id');
-- Or run individual steps:
--   PERFORM stg.refresh_gl_accounts('company-id');
--   PERFORM stg.refresh_journal_entries('company-id');
--   PERFORM mart.refresh_gl_accounts('company-id');
--   PERFORM mart.refresh_account_balances('company-id');
--   PERFORM mart.refresh_income_statement('company-id');
--   PERFORM mart.refresh_balance_sheet('company-id');
--   PERFORM mart.refresh_ebitda('company-id');
--
-- Classification: populate cfg.account_classification_rules per company
-- before running.  See docs/native-bi-accounting-mart.md.
-- ============================================================

-- ── mart.ebitda_summary table ────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS mart.ebitda_summary (
    company_id       TEXT          NOT NULL,
    period_year      INTEGER       NOT NULL,
    period_month     INTEGER       NOT NULL,
    revenue          NUMERIC(18,4) DEFAULT 0,
    cogs             NUMERIC(18,4) DEFAULT 0,
    gross_profit     NUMERIC(18,4) DEFAULT 0,
    opex             NUMERIC(18,4) DEFAULT 0,
    ebitda           NUMERIC(18,4) DEFAULT 0,
    depreciation     NUMERIC(18,4) DEFAULT 0,
    amortization     NUMERIC(18,4) DEFAULT 0,
    financial_result NUMERIC(18,4) DEFAULT 0,
    tax_result       NUMERIC(18,4) DEFAULT 0,
    net_income       NUMERIC(18,4) DEFAULT 0,
    refreshed_at     TIMESTAMPTZ   DEFAULT NOW(),
    PRIMARY KEY (company_id, period_year, period_month)
);

CREATE INDEX IF NOT EXISTS ix_mart_ebitda_period
    ON mart.ebitda_summary (company_id, period_year, period_month);

-- ── stg.refresh_gl_accounts ──────────────────────────────────────────────────
-- raw.sap_oact (full-refresh) → stg.gl_account
-- Converts Y/tYES/YES booleans; "Levels" VARCHAR → INTEGER

CREATE OR REPLACE FUNCTION stg.refresh_gl_accounts(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'raw' AND table_name = 'sap_oact'
    ) THEN
        RETURN;
    END IF;

    INSERT INTO stg.gl_account (
        company_id, code, name, father_num, level, group_mask, account_type,
        postable, frozen, valid_for, cash_account, control_account,
        currency, format_code, external_code, extracted_at_utc, transformed_at_utc
    )
    SELECT
        company_id,
        "Code",
        "Name",
        "FatherNum",
        CASE WHEN "Levels" ~ '^\d+$' THEN "Levels"::INTEGER ELSE NULL END,
        "GroupMask",
        "AccountType",
        CASE UPPER(TRIM(COALESCE("Postable",      ''))) WHEN 'Y' THEN TRUE WHEN 'TYES' THEN TRUE WHEN 'YES' THEN TRUE WHEN 'TRUE' THEN TRUE ELSE FALSE END,
        CASE UPPER(TRIM(COALESCE("Frozen",        ''))) WHEN 'Y' THEN TRUE WHEN 'TYES' THEN TRUE WHEN 'YES' THEN TRUE WHEN 'TRUE' THEN TRUE ELSE FALSE END,
        CASE UPPER(TRIM(COALESCE("ValidFor",      ''))) WHEN 'Y' THEN TRUE WHEN 'TYES' THEN TRUE WHEN 'YES' THEN TRUE WHEN 'TRUE' THEN TRUE ELSE FALSE END,
        CASE UPPER(TRIM(COALESCE("CashAccount",   ''))) WHEN 'Y' THEN TRUE WHEN 'TYES' THEN TRUE WHEN 'YES' THEN TRUE WHEN 'TRUE' THEN TRUE ELSE FALSE END,
        CASE UPPER(TRIM(COALESCE("ControlAccount",''))) WHEN 'Y' THEN TRUE WHEN 'TYES' THEN TRUE WHEN 'YES' THEN TRUE WHEN 'TRUE' THEN TRUE ELSE FALSE END,
        "Currency",
        "FormatCode",
        "ExternalCode",
        extracted_at_utc,
        NOW()
    FROM "raw"."sap_oact"
    WHERE company_id = p_company_id
    ON CONFLICT (company_id, code) DO UPDATE SET
        name               = EXCLUDED.name,
        father_num         = EXCLUDED.father_num,
        level              = EXCLUDED.level,
        group_mask         = EXCLUDED.group_mask,
        account_type       = EXCLUDED.account_type,
        postable           = EXCLUDED.postable,
        frozen             = EXCLUDED.frozen,
        valid_for          = EXCLUDED.valid_for,
        cash_account       = EXCLUDED.cash_account,
        control_account    = EXCLUDED.control_account,
        currency           = EXCLUDED.currency,
        format_code        = EXCLUDED.format_code,
        external_code      = EXCLUDED.external_code,
        extracted_at_utc   = EXCLUDED.extracted_at_utc,
        transformed_at_utc = NOW();
END;
$func$;

-- ── stg.refresh_journal_entries ───────────────────────────────────────────────
-- raw.sap_ojdt → stg.journal_entry
-- raw.sap_jdt1 → stg.journal_entry_line

CREATE OR REPLACE FUNCTION stg.refresh_journal_entries(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    -- Headers
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_ojdt') THEN
        INSERT INTO stg.journal_entry (
            company_id, trans_id, jdt_num, ref_date, due_date, tax_date,
            memo, trans_type, base_ref, user_ref, created_by,
            extracted_at_utc, transformed_at_utc
        )
        SELECT company_id, "TransId", "JdtNum",
               "RefDate", "DueDate", "TaxDate",
               "Memo", "TransType", "BaseRef", "UserRef", "CreatedBy",
               extracted_at_utc, NOW()
        FROM "raw"."sap_ojdt"
        WHERE company_id = p_company_id
        ON CONFLICT (company_id, trans_id) DO UPDATE SET
            jdt_num            = EXCLUDED.jdt_num,
            ref_date           = EXCLUDED.ref_date,
            due_date           = EXCLUDED.due_date,
            tax_date           = EXCLUDED.tax_date,
            memo               = EXCLUDED.memo,
            trans_type         = EXCLUDED.trans_type,
            base_ref           = EXCLUDED.base_ref,
            user_ref           = EXCLUDED.user_ref,
            created_by         = EXCLUDED.created_by,
            extracted_at_utc   = EXCLUDED.extracted_at_utc,
            transformed_at_utc = NOW();
    END IF;

    -- Lines
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_jdt1') THEN
        INSERT INTO stg.journal_entry_line (
            company_id, trans_id, line_id, account,
            debit, credit, fc_debit, fc_credit, sys_debit, sys_credit,
            short_name, contra_act, line_memo, ref_date,
            profit_code, ocr_code, ocr_code2, ocr_code3, ocr_code4, ocr_code5,
            project_code, extracted_at_utc, transformed_at_utc
        )
        SELECT company_id, "TransId", "LineId", "Account",
               "Debit", "Credit", "FcDebit", "FcCredit", "SysDebit", "SysCredit",
               "ShortName", "ContraAct", "LineMemo", "RefDate",
               "ProfitCode", "OcrCode", "OcrCode2", "OcrCode3", "OcrCode4", "OcrCode5",
               "ProjectCode", extracted_at_utc, NOW()
        FROM "raw"."sap_jdt1"
        WHERE company_id = p_company_id
        ON CONFLICT (company_id, trans_id, line_id) DO UPDATE SET
            account = EXCLUDED.account, debit = EXCLUDED.debit, credit = EXCLUDED.credit,
            fc_debit = EXCLUDED.fc_debit, fc_credit = EXCLUDED.fc_credit,
            sys_debit = EXCLUDED.sys_debit, sys_credit = EXCLUDED.sys_credit,
            short_name = EXCLUDED.short_name, contra_act = EXCLUDED.contra_act,
            line_memo = EXCLUDED.line_memo, ref_date = EXCLUDED.ref_date,
            profit_code = EXCLUDED.profit_code, ocr_code = EXCLUDED.ocr_code,
            ocr_code2 = EXCLUDED.ocr_code2, ocr_code3 = EXCLUDED.ocr_code3,
            ocr_code4 = EXCLUDED.ocr_code4, ocr_code5 = EXCLUDED.ocr_code5,
            project_code = EXCLUDED.project_code,
            extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
    END IF;
END;
$func$;

-- ── mart.refresh_gl_accounts ─────────────────────────────────────────────────
-- stg.gl_account + cfg.account_classification_rules → mart.gl_accounts
-- Classification priority: (1) exact code, (2) format_code prefix, (3) SAP fallback

CREATE OR REPLACE FUNCTION mart.refresh_gl_accounts(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    INSERT INTO mart.gl_accounts (
        company_id, code, name, father_num, level, account_type,
        statement_line, postable, currency, refreshed_at
    )
    SELECT
        g.company_id, g.code, g.name, g.father_num, g.level, g.account_type,
        COALESCE(
            (SELECT r.statement_line FROM cfg.account_classification_rules r
             WHERE r.company_id = g.company_id AND r.account_code = g.code LIMIT 1),
            (SELECT r.statement_line FROM cfg.account_classification_rules r
             WHERE r.company_id = g.company_id AND r.account_code IS NULL
               AND r.format_code IS NOT NULL AND g.format_code LIKE r.format_code || '%' LIMIT 1),
            CASE g.account_type
                WHEN 'act_AccountsReceivable' THEN 'current_assets'
                WHEN 'act_AccountsPayable'   THEN 'current_liabilities'
                WHEN 'act_Sales'             THEN 'revenue'
                WHEN 'act_Expense'           THEN 'opex'
                WHEN 'act_FixedAssets'       THEN 'non_current_assets'
                ELSE 'unclassified'
            END
        ) AS statement_line,
        g.postable, g.currency, NOW()
    FROM stg.gl_account g
    WHERE g.company_id = p_company_id
    ON CONFLICT (company_id, code) DO UPDATE SET
        name = EXCLUDED.name, father_num = EXCLUDED.father_num, level = EXCLUDED.level,
        account_type = EXCLUDED.account_type, statement_line = EXCLUDED.statement_line,
        postable = EXCLUDED.postable, currency = EXCLUDED.currency, refreshed_at = NOW();
END;
$func$;

-- ── mart.refresh_gl_accounts_from_journal_lines ──────────────────────────────
-- Injects JDT1 orphan accounts (posting accounts in JDT1 not present in OACT/stg.gl_account)
-- into mart.gl_accounts with PCGE prefix-based classification.
-- Called after mart.refresh_gl_accounts to ensure JDT1 accounts survive each pipeline refresh.
-- Idempotent: ON CONFLICT (company_id, code) DO UPDATE reclassifies on each run.

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

-- ── mart.refresh_account_balances ────────────────────────────────────────────
-- stg.journal_entry_line → mart.account_balances (monthly debit/credit sums per account)

CREATE OR REPLACE FUNCTION mart.refresh_account_balances(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    INSERT INTO mart.account_balances (
        company_id, code, period_year, period_month, debit_sum, credit_sum, refreshed_at
    )
    SELECT
        company_id,
        account                              AS code,
        EXTRACT(YEAR  FROM ref_date)::INTEGER AS period_year,
        EXTRACT(MONTH FROM ref_date)::INTEGER AS period_month,
        SUM(COALESCE(debit,  0))             AS debit_sum,
        SUM(COALESCE(credit, 0))             AS credit_sum,
        NOW()
    FROM stg.journal_entry_line
    WHERE company_id = p_company_id
      AND account IS NOT NULL AND ref_date IS NOT NULL
    GROUP BY company_id, account,
             EXTRACT(YEAR FROM ref_date), EXTRACT(MONTH FROM ref_date)
    ON CONFLICT (company_id, code, period_year, period_month) DO UPDATE SET
        debit_sum = EXCLUDED.debit_sum, credit_sum = EXCLUDED.credit_sum, refreshed_at = NOW();
END;
$func$;

-- ── mart.refresh_income_statement ────────────────────────────────────────────
-- mart.account_balances + mart.gl_accounts → mart.income_statement_summary
-- PCGE Peru sign convention (executive positive display):
--   revenue / other_income / financial = credit - debit  (positive = income)
--   cogs / opex / other_expense / tax  = ABS(debit - credit) (always positive cost)

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

-- ── mart.refresh_balance_sheet ───────────────────────────────────────────────
-- mart.account_balances + mart.gl_accounts → mart.balance_sheet_summary
-- snapshot_date = last day of each month

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

-- ── mart.refresh_ebitda ──────────────────────────────────────────────────────
-- mart.income_statement_summary → mart.ebitda_summary
-- depreciation/amortization = 0 until client classifies via cfg.account_classification_rules

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

-- ── mart.refresh_accounting_all ──────────────────────────────────────────────
-- Orchestrator — runs all 7 ETL steps; returns execution log per step.
-- Usage: SELECT * FROM mart.refresh_accounting_all('company-id');

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
