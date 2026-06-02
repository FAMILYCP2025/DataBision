# SAP B1 Service Layer — Validation Plan

**Version:** 1.0  
**Date:** 2026-05-31  
**Status:** Pending execution  
**Scope:** Connectivity and functional validation before Sprint E1 implementation begins  
**Reference:** `docs/sap-extractor-architecture.md` — Mode C (Service Layer Polling)

> **Purpose of this document:** Validate that the SAP B1 Service Layer instance used for the first customer is behaving exactly as the extractor architecture assumes. Many behavior differences between SAP B1 versions, partner customizations, and HANA configurations are only discovered at runtime. This plan surfaces those differences before writing implementation code.

---

## 1. Prerequisites

### 1.1 Environment Required

| Item | Requirement |
|---|---|
| SAP B1 version | Must be known. Minimum: 9.0. Service Layer features differ by version. |
| Service Layer URL | HTTPS preferred. HTTP acceptable for validation only. |
| SAP B1 user | `DBI_READER` or equivalent — read-only, Professional User type. |
| Network access | Validator machine must reach SL port (50000 or customer-defined). |
| Reference data | At least 100 invoices (OINV) with a range of `UpdateDate` values. Ideally 3+ months of data. |
| Tools | `curl` or Postman. No code required for this plan — all tests are manual HTTP requests. |
| DataBision Ingest API | Running and accepting `dev-key-001`. Used in §8 (end-to-end flow). |

### 1.2 Information to Gather Before Tests

Before executing any test, document the following from the SAP B1 system:

| Field | Where to Find | Required for |
|---|---|---|
| SAP B1 version (e.g. 10.0.195) | SL metadata: `GET /b1s/v1/$metadata` | All version-specific behaviors |
| Service Layer version | Same response, `Version` field in Login | OData dialect |
| Database type | Login: check if HANA or SQL Server flavor | Date format in filters |
| `CompanyDB` name | SAP B1 admin / connection string | Login payload |
| Session timeout setting | Login response: `SessionTimeout` field | §2 tests |
| Total row count OINV | `GET /b1s/v1/Invoices?$count=true&$top=0` | §4 tests |
| Total row count OCRD | `GET /b1s/v1/BusinessPartners?$count=true&$top=0` | §5 tests |
| Total row count OITM | `GET /b1s/v1/Items?$count=true&$top=0` | §6 tests |
| Total row count OSLP | `GET /b1s/v1/SalesPersons?$count=true&$top=0` | §7 tests |
| `UpdateDate` format returned | Any GET with `$select=UpdateDate` | Filter syntax |
| `UpdateDate` filter syntax accepted | See §4.2 | Filter construction |

---

## 2. Test Area: Login and Session

### 2.1 Basic Login

**Test V-LOGIN-01: Login with valid credentials**

```
POST {sl_base_url}/Login
Content-Type: application/json

{
  "CompanyDB": "{company_db}",
  "UserName":  "{username}",
  "Password":  "{password}"
}
```

**Expected:**
- HTTP 200
- Body contains `SessionId` (non-empty string)
- Body contains `SessionTimeout` (integer, expected ≥ 28)
- Response sets `B1SESSION={SessionId}` cookie
- Response time < 3 seconds

**Record:**
- Exact `SessionTimeout` value returned
- Body structure (some versions return `odata.metadata`, others `@odata.context`)
- Cookie name and format (variations: `B1SESSION`, `B1SESSION2`, etc.)
- Response time in ms

---

**Test V-LOGIN-02: Login with invalid credentials**

```
POST {sl_base_url}/Login
{
  "CompanyDB": "{company_db}",
  "UserName": "invalid_user",
  "Password": "wrong_password"
}
```

**Expected:**
- HTTP 401 or HTTP 400
- Body contains an error message (not a generic 500)
- No `B1SESSION` cookie set
- Response time < 5 seconds (not a hang)

**Record:**
- Exact HTTP status code
- Exact error body structure
- Whether it hangs or responds quickly

---

**Test V-LOGIN-03: Login with invalid CompanyDB**

```
POST {sl_base_url}/Login
{
  "CompanyDB": "NONEXISTENT_DB",
  "UserName": "{username}",
  "Password": "{password}"
}
```

**Expected:**
- HTTP 400 or HTTP 404 (not 200)
- Informative error body

**Record:**
- HTTP status code
- Error body

---

**Test V-LOGIN-04: Logout**

Prerequisite: valid `B1SESSION` cookie from V-LOGIN-01.

```
POST {sl_base_url}/Logout
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 204 No Content (some versions: 200)
- Session is invalidated — subsequent requests with the same cookie return 401

**Verify invalidation:**
```
GET {sl_base_url}/Invoices?$top=1
Cookie: B1SESSION={session_id}
```
Expected: HTTP 401 after logout.

---

### 2.2 Session Behavior

**Test V-SESSION-01: Session cookie reuse**

Using the cookie from V-LOGIN-01, make 10 consecutive requests:
```
GET {sl_base_url}/Invoices?$top=1
Cookie: B1SESSION={session_id}
```

**Expected:**
- All 10 requests return HTTP 200
- No re-authentication required
- Response times stable (< 500ms each after first)

**Record:**
- Response time per request (ms)
- Whether any request returns 401 unexpectedly

---

**Test V-SESSION-02: Session expiry detection**

> Note: This test is time-consuming. Execute only if `SessionTimeout` is ≤ 5 minutes (some test/demo systems have short timeouts). Skip if SessionTimeout = 30 min.

Wait `SessionTimeout + 2 minutes` after login, then:
```
GET {sl_base_url}/Invoices?$top=1
Cookie: B1SESSION={expired_session_id}
```

**Expected:**
- HTTP 401 with body indicating session expired
- Specific error code or message (document the exact body)

**Record:**
- Exact HTTP status and body when session expires

---

**Test V-SESSION-03: Multiple concurrent sessions**

Execute two logins in quick succession (separate tools or terminal tabs). Use each session for a request simultaneously.

**Expected:**
- Both sessions work independently
- No session collision or interference
- Confirms extractor can use a single session per run without cross-session issues

---

## 3. Test Area: Metadata and Schema Discovery

### 3.1 OData Metadata

**Test V-META-01: Fetch OData metadata**

```
GET {sl_base_url}/$metadata
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- XML response with entity type definitions
- Contains `Invoices`, `BusinessPartners`, `Items`, `SalesPersons` entity sets
- Response time < 10 seconds (metadata can be large)

**Record:**
- SAP B1 version string from metadata
- Whether `UpdateDate` appears as a property in `Invoices` entity type
- Data type of `UpdateDate` (`Edm.DateTime`, `Edm.Date`, or `Edm.String`)
- Whether `UpdateTS` appears and its type
- Full property list for each of the 4 entities (compare against `$select` lists in architecture doc)

---

**Test V-META-02: Field presence check for OINV**

```
GET {sl_base_url}/Invoices?$top=1&$select=DocEntry,DocNum,CardCode,DocDate,DocDueDate,TaxDate,DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- Response contains all requested fields
- No `400 Bad Request` for unknown field names

**Record:**
- Any fields that return 400 (need to be removed from `$select`)
- Actual field names returned (case may differ)
- Data type format for `UpdateDate` (e.g. `"2026-01-15T00:00:00Z"` vs `"2026-01-15"` vs `20260115`)

---

**Test V-META-03: Field presence check for OCRD, OITM, OSLP**

Repeat V-META-02 for:

- OCRD: `GET /b1s/v1/BusinessPartners?$top=1&$select=CardCode,CardName,CardType,GroupCode,ContactPerson,Phone1,Phone2,Currency,SalesPersonCode,VatLiable,FederalTaxID,FrozenFor,CurrentAccountBalance,CreditLimit,CreateDate,UpdateDate`
- OITM: `GET /b1s/v1/Items?$top=1&$select=ItemCode,ItemName,ItemsGroupCode,QuantityOnStock,CommittedQuantity,OrderedQuantity,AverageCost,LastPurchasePrice,CreateDate,UpdateDate`
- OSLP: `GET /b1s/v1/SalesPersons?$top=1&$select=SalesEmployeeCode,SalesEmployeeName,CreateDate,UpdateDate`

**Record per entity:**
- Which fields exist vs which return 400
- Actual field names (case differences matter)
- Data types

---

## 4. Test Area: OINV Incremental Extraction

### 4.1 Basic Read

**Test V-OINV-01: Fetch first 10 invoices**

```
GET {sl_base_url}/Invoices?$top=10&$orderby=DocEntry asc
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- JSON response with `value` array containing ≤ 10 objects
- Each object has `DocEntry` (integer), `UpdateDate` (some date format)
- Response time < 2 seconds

**Record:**
- Exact JSON structure of a single invoice object
- Exact format of `UpdateDate` (critical for filter construction)
- Whether `UpdateDate` is a string, ISO date, or SAP-specific format
- Presence or absence of `UpdateTS` field and its format

---

**Test V-OINV-02: Verify `$filter` on UpdateDate with ISO format**

Determine a date that should return some invoices. Use a date 30 days ago:

```
GET {sl_base_url}/Invoices?$filter=UpdateDate ge '2026-04-30'&$top=10&$select=DocEntry,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200 (not 400)
- At least 1 result (verify against known data)
- If 400: document the error — the date format may need adjustment

**Variations to try if V-OINV-02 fails:**
```
# SAP format (some versions)
$filter=UpdateDate ge '20260430'
# With timestamp
$filter=UpdateDate ge datetime'2026-04-30T00:00:00'
# With Edm.Date annotation
$filter=UpdateDate ge 2026-04-30
```

**Record:**
- Which date format is accepted
- Exact error body if all formats fail (indicates SL version issue)

---

**Test V-OINV-03: Verify `$orderby` on UpdateDate + DocEntry**

```
GET {sl_base_url}/Invoices?$filter=UpdateDate ge '2026-01-01'&$top=20&$select=DocEntry,UpdateDate&$orderby=UpdateDate asc,DocEntry asc
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- Results sorted by UpdateDate ascending, then DocEntry ascending
- No 400 or 501 (some SL versions do not support composite orderby)

**Record:**
- Whether composite `$orderby` on two fields is supported
- If not, try single-field `$orderby=DocEntry asc`

---

**Test V-OINV-04: Watermark-based incremental filter**

Define a realistic watermark: last 7 days.

```
GET {sl_base_url}/Invoices
  ?$filter=UpdateDate ge '2026-05-24'
  &$select=DocEntry,UpdateDate
  &$orderby=UpdateDate asc,DocEntry asc
  &$top=500
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- Result set matches the known number of invoices modified in the last 7 days
- Compare against a direct SAP B1 query or known count
- Response time < 5 seconds for < 1000 rows

**Metrics:**
- Row count returned
- Response time (ms)
- Whether the result count matches expectations (validates the filter logic)

---

**Test V-OINV-05: Full `$select` payload**

```
GET {sl_base_url}/Invoices?$filter=UpdateDate ge '2026-05-24'&$top=5&$select=DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- All fields present
- No null for required fields (DocEntry, DocNum, CardCode, UpdateDate)
- Numeric fields are numbers (not strings)
- Date fields in consistent format

**Record:**
- Any fields missing from response despite being in `$select`
- Null rate for optional fields (Comments, Cancelled)
- Exact type of `DocTotal` (string vs number)

---

## 5. Test Area: OCRD Incremental Extraction

**Test V-OCRD-01: Fetch first 5 business partners**

```
GET {sl_base_url}/BusinessPartners?$top=5&$select=CardCode,CardName,CardType,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- `CardCode` is a string (e.g. `"C0001"`)
- `UpdateDate` present and parseable
- `CardType` is `C` (Customer) or `S` (Supplier) or `L` (Lead)

**Record:**
- Exact format of `CardCode` (length, prefix pattern)
- `CardType` values present in the dataset
- Whether `UpdateDate` format matches OINV format

---

**Test V-OCRD-02: UpdateDate filter for OCRD**

Same filter format as V-OINV-02 but applied to BusinessPartners:
```
GET {sl_base_url}/BusinessPartners?$filter=UpdateDate ge '2026-04-30'&$top=10&$select=CardCode,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200 with same filter syntax as OINV
- If filter format differs from OINV — document the difference

**Record:**
- Whether OCRD requires a different date format than OINV (uncommon but possible)

---

**Test V-OCRD-03: Full `$select` payload for OCRD**

```
GET {sl_base_url}/BusinessPartners?$top=3&$select=CardCode,CardName,CardType,GroupCode,ContactPerson,Phone1,Phone2,Currency,SalesPersonCode,VatLiable,FederalTaxID,FrozenFor,CurrentAccountBalance,CreditLimit,CreateDate,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- All fields present
- `CurrentAccountBalance` and `CreditLine` are numbers
- `FrozenFor` is `N` or `Y`

**Record:**
- Field name discrepancies (e.g. `CreditLimit` vs `CreditLine` vs `AllowancePct`)
- Whether `ContactPerson` is `ContactPerson` or `ContactPerson` (verify exact API name)
- Null rate for optional fields

---

## 6. Test Area: OITM Incremental Extraction

**Test V-OITM-01: Fetch first 5 items**

```
GET {sl_base_url}/Items?$top=5&$select=ItemCode,ItemName,ItemsGroupCode,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- `ItemCode` is a string
- `ItemsGroupCode` is an integer (or a string — document the actual type)
- `UpdateDate` present

**Record:**
- Exact type of `ItemsGroupCode` (critical: the DTO has it as `string?` but DB column is `INTEGER`)
- Whether `ItemCode` is always uppercase

---

**Test V-OITM-02: UpdateDate filter for OITM**

```
GET {sl_base_url}/Items?$filter=UpdateDate ge '2026-04-30'&$top=10&$select=ItemCode,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200 with expected row count

**Record:**
- Count of items modified in last 30 days (baseline for incremental)

---

**Test V-OITM-03: Full `$select` payload for OITM**

```
GET {sl_base_url}/Items?$top=3&$select=ItemCode,ItemName,ItemsGroupCode,QuantityOnStock,CommittedQuantity,OrderedQuantity,AverageCost,LastPurchasePrice,CreateDate,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- Numeric fields are numbers, not strings
- No unexpected nulls for required fields

**Record:**
- Field name discrepancies
- Whether `QuantityOnStock` changes frequently (relevant for watermark lag analysis)

---

## 7. Test Area: OSLP Incremental Extraction

**Test V-OSLP-01: Fetch all salespersons**

```
GET {sl_base_url}/SalesPersons?$select=SalesEmployeeCode,SalesEmployeeName,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200
- Row count matches known number of salespersons (typically < 100)
- `SalesEmployeeCode` is an integer
- Single page (no pagination needed)

**Record:**
- Total count of salespersons
- Format of `SalesEmployeeCode` (integer vs string)
- Whether a `$filter=UpdateDate` filter is supported (or if full-refresh is the only option)

---

**Test V-OSLP-02: UpdateDate filter for OSLP**

```
GET {sl_base_url}/SalesPersons?$filter=UpdateDate ge '2026-01-01'&$select=SalesEmployeeCode,UpdateDate
Cookie: B1SESSION={session_id}
```

**Expected:**
- HTTP 200 (some SL versions may not support UpdateDate filter on this entity)
- If 400: document the error — OSLP may require full-refresh strategy

**Record:**
- Whether `$filter` on `UpdateDate` is supported for this entity
- If not supported, confirm full nightly refresh is viable (entity is small)

---

## 8. Test Area: Pagination

### 8.1 `$top` + `$skip` Behavior

**Test V-PAGE-01: Basic pagination (OINV)**

```
# Page 1
GET {sl_base_url}/Invoices?$top=5&$skip=0&$orderby=DocEntry asc&$select=DocEntry

# Page 2
GET {sl_base_url}/Invoices?$top=5&$skip=5&$orderby=DocEntry asc&$select=DocEntry

# Page 3
GET {sl_base_url}/Invoices?$top=5&$skip=10&$orderby=DocEntry asc&$select=DocEntry
```

**Expected:**
- No DocEntry repeated across pages
- No DocEntry missing between pages (contiguous sequence)
- Page 3 DocEntry values are strictly higher than page 1

**Record:**
- Actual DocEntry values on each page
- Whether `$skip` is honored exactly

---

**Test V-PAGE-02: Last page detection**

```
# Request more rows than available in a filtered window (choose a narrow date range)
GET {sl_base_url}/Invoices?$filter=UpdateDate ge '2026-05-30'&$top=500&$skip=0&$select=DocEntry
```

**Expected:**
- HTTP 200
- Row count < 500 if fewer items exist (confirms last-page detection by `rows < $top`)
- OR: response includes `odata.nextLink` absent (confirms end)

**Record:**
- Whether SL returns a `odata.nextLink` or `@odata.nextLink` header/field
- Whether the last page has exactly `rows < $top` or some other signal
- This determines the pagination loop termination condition in code

---

**Test V-PAGE-03: `$skip` performance at scale**

> Note: Only execute if OINV has more than 5,000 rows total.

```
# Page 1 (rows 0-499)
GET {sl_base_url}/Invoices?$top=500&$skip=0&$orderby=DocEntry asc&$select=DocEntry,UpdateDate
# Record response time

# Page 10 (rows 4500-4999)
GET {sl_base_url}/Invoices?$top=500&$skip=4500&$orderby=DocEntry asc&$select=DocEntry,UpdateDate
# Record response time

# Page 20 (rows 9500-9999)
GET {sl_base_url}/Invoices?$top=500&$skip=9500&$orderby=DocEntry asc&$select=DocEntry,UpdateDate
# Record response time
```

**Expected:**
- Response times do not degrade linearly with skip value (or degradation is acceptable)
- Acceptable: < 2s at skip=9500

**Record:**
- Response time at skip=0, skip=4500, skip=9500
- If degradation > 3× at skip=9500 vs skip=0: flag as risk — keyset pagination needed for initial load

---

**Test V-PAGE-04: `$count` support**

```
GET {sl_base_url}/Invoices/$count
Cookie: B1SESSION={session_id}

# Alternative
GET {sl_base_url}/Invoices?$count=true&$top=0
```

**Expected:**
- HTTP 200 with an integer (total row count)
- Useful for estimating initial load duration

**Record:**
- Total OINV count
- Whether `$count` is supported (some SL versions don't support it)
- If not supported, alternative to estimate count (not blocking — count is informational)

---

### 8.2 `nextLink` vs Row Count Termination

**Test V-PAGE-05: Determine pagination termination signal**

Execute a request where exactly one full page is expected (choose a date range with exactly 500 known rows, or use `$top=3` on a small entity):

```
GET {sl_base_url}/SalesPersons?$top=3&$skip=0&$select=SalesEmployeeCode
# (Assuming > 3 salespersons exist)
```

**Expected:**
- Check if `odata.nextLink` is present in the response body or as a response header
- This determines whether the extractor should use:
  - Row-count termination: `if (rows_returned < page_size) → last page`
  - `nextLink` termination: `if (no nextLink in response) → last page`

**Record:**
- Exact pagination signal returned by this SL instance

---

## 9. Test Area: Retry and Error Handling

### 9.1 Rate Limiting

**Test V-RETRY-01: Burst requests**

Send 20 requests in rapid succession (no pause between):
```
# Execute 20 consecutive GETs as fast as possible
GET {sl_base_url}/Invoices?$top=1
Cookie: B1SESSION={session_id}
```

**Expected:**
- Most return 200
- Some may return 429 (Too Many Requests) or degrade in response time
- No unexpected 500 errors
- No session invalidation

**Record:**
- How many 200 vs 429 vs other responses
- Whether 429 includes `Retry-After` header
- Response time degradation curve (first request vs 20th request)

---

**Test V-RETRY-02: Behavior after 429**

If V-RETRY-01 produced a 429:

After receiving 429, wait `Retry-After` seconds (or 60s if header absent), then:
```
GET {sl_base_url}/Invoices?$top=1
Cookie: B1SESSION={session_id}
```

**Expected:**
- Returns 200 after the wait
- Session remains valid (not invalidated by the 429)

**Record:**
- Whether SAP B1 returns `Retry-After` in 429 response
- Session validity after 429

---

### 9.2 Network Interruption Simulation

**Test V-RETRY-03: Long-running request interruption**

Start a request with a large `$skip` (which takes longer to respond) and interrupt it mid-flight:

1. Start: `GET /Invoices?$top=500&$skip=10000` (intentionally slow if dataset is large)
2. After 5 seconds: close the TCP connection (cancel in Postman / Ctrl-C in curl)
3. Immediately retry with the same request

**Expected:**
- Retry succeeds (returns 200)
- Session remains valid despite interrupted request
- No server-side state left over from the interrupted request

**Record:**
- Session behavior after a mid-flight TCP disconnect

---

### 9.3 Session Expiry During Extraction

**Test V-RETRY-04: Session used after simulated expiry**

> Only if you have a way to expire the session (e.g. test/demo SL with short timeout, or `POST /Logout` while another request is in flight).

1. Login, get session
2. `POST /Logout` (invalidates session)
3. Immediately use the now-invalid session:
   ```
   GET {sl_base_url}/Invoices?$top=1
   Cookie: B1SESSION={invalid_session_id}
   ```

**Expected:**
- HTTP 401
- Response body contains a message about invalid/expired session (document the exact body)
- This validates the re-login trigger condition in the extractor

**Record:**
- Exact HTTP status and response body on expired session request

---

## 10. Test Area: Timeout Behavior

**Test V-TIMEOUT-01: Large page request latency**

Measure the response time for progressively larger `$top` values:

```
GET /Invoices?$top=100&$select=DocEntry,UpdateDate   → record T100 ms
GET /Invoices?$top=500&$select=DocEntry,UpdateDate   → record T500 ms
GET /Invoices?$top=1000&$select=DocEntry,UpdateDate  → record T1000 ms
```

**Expected:**
- T100 < 1000ms
- T500 < 3000ms
- T1000 < 8000ms

**Acceptance criteria:**
- If T500 > 5000ms: use `$top=200` as the default page size
- If T1000 > 10000ms: do not use page sizes above 500

**Record:**
- Response time per page size
- Recommended page size based on results

---

**Test V-TIMEOUT-02: Full `$select` vs minimal `$select` latency**

Same `$top=500`, compare:
```
# Minimal
GET /Invoices?$top=500&$select=DocEntry,UpdateDate
→ Record T_minimal ms

# Full (as per architecture doc select list)
GET /Invoices?$top=500&$select=DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate
→ Record T_full ms
```

**Expected:**
- T_full is within 2× of T_minimal (Service Layer should not be significantly slower with more fields from a single row)

**Record:**
- T_minimal and T_full
- Ratio T_full / T_minimal
- If ratio > 3×: investigate — some SL configurations do extra lookups for certain fields

---

**Test V-TIMEOUT-03: Concurrent extraction of two objects**

Run two requests simultaneously:
```
# Terminal A (OINV)
GET /Invoices?$filter=UpdateDate ge '2026-05-01'&$top=500

# Terminal B (OCRD) — start at same time
GET /BusinessPartners?$filter=UpdateDate ge '2026-05-01'&$top=500
```

**Expected:**
- Both complete without error
- Response times are not significantly higher than sequential (< 1.5×)
- Same session can be used by both (session is stateless per request)

**Record:**
- Whether concurrent requests cause any issues
- Response time degradation under simultaneous load

---

## 11. Test Area: Performance Benchmarks

### 11.1 Row Throughput

**Test V-PERF-01: Throughput benchmark**

Fetch 5 consecutive pages of 500 rows each, measuring time:

```
# Measure total time for 5 pages of OINV (2500 rows)
Page 1: $top=500&$skip=0
Page 2: $top=500&$skip=500
Page 3: $top=500&$skip=1000
Page 4: $top=500&$skip=1500
Page 5: $top=500&$skip=2000
```

**Compute:**
- Rows per second = 2500 / total_time_seconds
- Average page latency = total_time_ms / 5

**Expected benchmarks (from architecture doc):**

| Metric | Expected | Acceptable | Fail |
|---|---|---|---|
| Rows/second | 200–500 | 100–200 | < 50 |
| Page latency (500 rows) | < 2s | 2–5s | > 5s |
| Full `$select` overhead vs minimal | < 50% | 50–100% | > 100% |

**Record:**
- Actual rows/second
- Average page latency
- Min/max page latency across the 5 pages

---

**Test V-PERF-02: Initial load time estimate**

Using V-PERF-01 measurements, compute the estimated initial load time for this customer:

```
Estimated time = (Total OINV rows / rows_per_second) seconds
```

Add 30% overhead for network variability and pagination overhead.

**Expected calculation:**

| OINV rows | At 300 rows/s | At 150 rows/s | At 50 rows/s |
|---|---|---|---|
| 10,000 | 33 s | 67 s | 200 s |
| 50,000 | 167 s (2.8 min) | 333 s (5.5 min) | 1000 s (17 min) |
| 200,000 | 667 s (11 min) | 1333 s (22 min) | 4000 s (67 min) |
| 500,000 | 1667 s (28 min) | 3333 s (55 min) | 10000 s (2.8 h) |

**Record:**
- Actual OINV row count
- Measured rows/second
- Estimated initial load time
- Assessment: acceptable for off-hours window? (< 4 hours is acceptable for initial load)

---

**Test V-PERF-03: Incremental run time estimate**

For a typical incremental window (last 2 hours):

```
GET /Invoices?$filter=UpdateDate ge '2026-05-31'&$count=true&$top=0
→ count_last_day

# Extrapolate: 2-hour window ≈ count_last_day / 12
```

**Record:**
- Estimated rows per 2-hour incremental window
- Estimated incremental extraction time at measured rows/second
- Assessment: fits in 30-min scheduling window? (target: < 5 minutes for typical incremental run)

---

## 12. Metrics Summary

The following metrics must be recorded and reported after executing all tests:

### 12.1 Environment Metrics

| Metric | Value |
|---|---|
| SAP B1 version | |
| Service Layer version | |
| Database type (HANA / SQL Server) | |
| Network type (LAN / VPN / Internet) | |
| Session timeout (minutes) | |

### 12.2 Connectivity Metrics

| Metric | Value |
|---|---|
| Login response time (ms) | |
| P50 request latency (ms) | |
| P95 request latency (ms) | |
| Max observed latency (ms) | |

### 12.3 Data Volume Metrics

| Object | Total rows | Rows in last 30 days | Rows in last 2 hours (est.) |
|---|---|---|---|
| OINV | | | |
| OCRD | | | |
| OITM | | | |
| OSLP | | | |

### 12.4 Performance Metrics

| Metric | Value |
|---|---|
| Rows/second (OINV, $top=500, full $select) | |
| Average page latency at $top=500 (ms) | |
| `$skip` degradation at skip=4500 vs skip=0 (% increase) | |
| Full `$select` overhead vs minimal `$select` (% increase) | |
| Recommended page size | |
| Estimated initial load time (hours) | |
| Estimated incremental run time (minutes) | |

### 12.5 Schema Compatibility

| Check | OINV | OCRD | OITM | OSLP |
|---|---|---|---|---|
| All `$select` fields present | | | | |
| `UpdateDate` filter works | | | | |
| `$orderby` works | | | | |
| Date format confirmed | | | | |
| Key field type confirmed | | | | |
| Field name discrepancies found | | | | |

### 12.6 Resilience Metrics

| Metric | Value |
|---|---|
| Rate limit threshold (requests/min before 429) | |
| `Retry-After` behavior on 429 | |
| Session validity after TCP disconnect | |
| HTTP status code on expired session | |
| Response body on expired session | |

---

## 13. Success Criteria

### 13.1 Mandatory (Go / No-Go for Sprint E1)

All of the following must pass before starting implementation:

| # | Criterion | Test |
|---|---|---|
| M1 | Login returns 200 with valid `B1SESSION` cookie | V-LOGIN-01 |
| M2 | Invalid credentials return 401/400 (not 500 or hang) | V-LOGIN-02 |
| M3 | `$filter=UpdateDate ge 'YYYY-MM-DD'` returns expected rows on OINV | V-OINV-02 |
| M4 | `$top=500` pagination returns consistent non-overlapping pages | V-PAGE-01 |
| M5 | Last page is detectable (row count < page_size OR nextLink absent) | V-PAGE-02, V-PAGE-05 |
| M6 | Full `$select` list for OINV returns 200 (all fields present) | V-OINV-05 |
| M7 | Rows/second ≥ 50 (minimum viable throughput) | V-PERF-01 |
| M8 | Initial load time estimate ≤ 8 hours | V-PERF-02 |
| M9 | 401 is returned on expired/invalid session (not 200 or 500) | V-RETRY-04 |

If **any mandatory criterion fails**, Sprint E1 implementation is blocked. The specific failure must be diagnosed and the architecture updated before proceeding.

### 13.2 Recommended (Affect implementation decisions)

These criteria inform implementation choices but do not block Sprint E1:

| # | Criterion | Impact if fails | Test |
|---|---|---|---|
| R1 | Rows/second ≥ 200 | Use smaller page size (200), estimate longer load window | V-PERF-01 |
| R2 | `$skip` degradation at skip=4500 < 2× vs skip=0 | Implement date-chunked pagination for initial load | V-PAGE-03 |
| R3 | Composite `$orderby=UpdateDate asc,DocEntry asc` works | Use single `$orderby=DocEntry asc` for consistency | V-OINV-03 |
| R4 | `$filter` works on OSLP `UpdateDate` | Use full nightly refresh (no impact on other objects) | V-OSLP-02 |
| R5 | Full `$select` overhead < 50% vs minimal | Minor performance impact, no architectural change | V-TIMEOUT-02 |
| R6 | 429 includes `Retry-After` header | Use fixed 60s backoff instead of dynamic | V-RETRY-01 |
| R7 | `$count` is supported | Cannot pre-estimate pages, minor UX impact only | V-PAGE-04 |
| R8 | Incremental run (2h window) < 5 minutes at measured throughput | Adjust scheduling frequency (30 min still viable up to 15 min runs) | V-PERF-03 |

---

## 14. Risks

### 14.1 Technical Risks

| ID | Risk | Probability | Impact | Detection | Mitigation |
|---|---|---|---|---|---|
| T1 | SAP B1 OData `$filter` on `UpdateDate` uses a non-standard date format | Medium | High — blocks all incremental extraction | V-OINV-02 | Test multiple formats (ISO, SAP YMD, datetime'...'); update filter builder per format |
| T2 | Service Layer does not support `$skip` pagination for some entities | Low | High — blocks initial load | V-PAGE-01 | Use `$filter` on date ranges to split large datasets without `$skip` |
| T3 | Throughput < 50 rows/s (SL on overloaded server) | Low | High — initial load exceeds maintenance window | V-PERF-01 | Negotiate off-hours window with customer; consider HANA SQL upgrade (Mode A) |
| T4 | Session timeout configured to < 5 minutes on test/demo system | Medium | Medium — causes frequent re-logins | V-SESSION-02 | Architecture handles this via `SessionManager.InvalidateAsync()`; verify against production SL |
| T5 | `UpdateDate` filter returns 0 results despite known data | Medium | High — incremental returns nothing | V-OINV-04 | Compare against SAP B1 query directly; may indicate `UpdateDate` not being updated by some operations |
| T6 | Field names differ between SL version and architecture doc (e.g. `SalesPersonCode` vs `SlpCode`) | High | Medium — requires field mapping update | V-META-01/02/03 | This is expected — the validation plan is designed to surface this; update `SapFieldMapper.cs` before implementation |
| T7 | `UpdateDate` not available/not reliable for OSLP | Medium | Low — OSLP is small, full refresh is acceptable | V-OSLP-02 | Switch to nightly full refresh strategy for OSLP |
| T8 | SAP B1 applies row-level permissions and read-only user cannot see some invoices | Low | High — silent data gaps | Post-validation: compare counts via SAP client | Ensure `DBI_READER` has full read access to OINV/OCRD/OITM/OSLP with no filters |
| T9 | Multiple concurrent sessions rejected by SAP B1 license | Low | Medium — limits parallelism | V-SESSION-03 | Use single session (architecture default), no parallel objects by default |
| T10 | `$skip` O(n²) degradation makes initial load impractical | Medium (> 100k rows) | Medium — increases load time | V-PAGE-03 | Implement date-chunked initial load in Sprint E1 (avoid $skip > 5000) |

### 14.2 Business / Operational Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| B1 | SAP B1 Professional User license required for Service Layer | Blocks onboarding | Confirm license type before installation; include in onboarding checklist |
| B2 | Customer firewall blocks port 50000 from extractor server to SAP server | Blocks all SL access | Confirm firewall rules during onboarding; document required rules |
| B3 | Self-signed SSL certificate on SAP SL — extractor must trust it | Security policy conflict | Validate certificate chain; use `TrustStorePath` with CA cert; document risk if `SslValidateCertificate=false` required |
| B4 | SAP B1 server overloaded during business hours — extraction degrades | Incremental runs take too long | Schedule incremental for off-peak; add configurable pause between pages |
| B5 | Customer does not want read user created in B1 (policy) | Blocks Mode C | Discuss alternative (dedicated reporting schema, read-only role) with customer and SAP partner |

---

## 15. Execution Checklist

Execute tests in this order (each section depends on the previous):

```
[ ] 1. Gather environment information (§1.2)
[ ] 2. V-LOGIN-01: Basic login
[ ] 3. V-LOGIN-02: Invalid credentials
[ ] 4. V-META-01: OData metadata
[ ] 5. V-META-02: OINV field presence
[ ] 6. V-META-03: OCRD/OITM/OSLP field presence
[ ] 7. V-SESSION-01: Session reuse
[ ] 8. V-OINV-01: Basic OINV read
[ ] 9. V-OINV-02: UpdateDate filter ← MANDATORY M3
[ ] 10. V-OINV-03: Composite orderby
[ ] 11. V-OINV-04: Watermark-based filter
[ ] 12. V-OINV-05: Full $select payload
[ ] 13. V-OCRD-01/02/03: OCRD tests
[ ] 14. V-OITM-01/02/03: OITM tests
[ ] 15. V-OSLP-01/02: OSLP tests
[ ] 16. V-PAGE-01: Basic pagination
[ ] 17. V-PAGE-02: Last page detection ← MANDATORY M5
[ ] 18. V-PAGE-03: $skip degradation at scale (if > 5000 rows)
[ ] 19. V-PAGE-04: $count support
[ ] 20. V-PAGE-05: nextLink vs row-count signal
[ ] 21. V-TIMEOUT-01: Page size latency
[ ] 22. V-TIMEOUT-02: $select overhead
[ ] 23. V-TIMEOUT-03: Concurrent objects
[ ] 24. V-PERF-01: Throughput benchmark ← MANDATORY M7
[ ] 25. V-PERF-02: Initial load time estimate ← MANDATORY M8
[ ] 26. V-PERF-03: Incremental run time estimate
[ ] 27. V-RETRY-01: Burst requests
[ ] 28. V-RETRY-04: Expired session response ← MANDATORY M9
[ ] 29. V-LOGIN-04: Logout
[ ] 30. Fill in §12 Metrics Summary
[ ] 31. Evaluate §13 Success Criteria
[ ] 32. Document all risks from §14 that were confirmed
[ ] 33. Deliver findings report (see §16)
```

---

## 16. Findings Report Template

After executing all tests, produce a findings report with this structure:

```
# Service Layer Validation Findings — {Customer} — {Date}

## Environment
- SAP B1 version:
- SL version:
- DB type:
- Network:

## Go/No-Go Decision
[ ] GO — all mandatory criteria passed
[ ] NO-GO — blocked by: {list}

## Mandatory Criteria Results
| Criterion | Result | Notes |
|---|---|---|
| M1 Login | PASS/FAIL | |
| ... | | |

## Recommended Criteria Results
| Criterion | Result | Decision |
|---|---|---|
| R1 Throughput | PASS/FAIL/PARTIAL | page_size=X |
| ... | | |

## Schema Discrepancies Found
(List field names that differ from architecture doc)

## Recommended Implementation Adjustments
(Based on findings — specific changes to SapFieldMapper.cs, page size, date format, etc.)

## Performance Summary
- Rows/second: X
- Estimated initial load: Y hours
- Estimated incremental (2h window): Z minutes

## Confirmed Risks
(From §14 — which risks materialized)

## Open Questions
(Anything that needs follow-up before Sprint E1)
```

---

*Validation Plan v1.0 — DataBision SAP Extractor Mode C*  
*Reference: `docs/sap-extractor-architecture.md`*  
*This plan must be fully executed and the Go/No-Go decision recorded before Sprint E1 begins.*
