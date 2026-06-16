# ksdepor Additional SAP Objects — Sprint 8J Results

Date: 2026-06-15  
Company: `company-dev-001` (CLTSTKSDEPOR)  
SAP Business One SL version: 1000290

## Objects activated

| Object | SL endpoint | $select (final) | Rows extracted | Status |
|---|---|---|---|---|
| OPOR | PurchaseOrders | DocEntry,DocNum,DocDate,DocDueDate,CardCode,CardName,DocTotal,VatSum,Cancelled,SalesPersonCode,UpdateDate | 20 | SUCCESS |
| OPDN | PurchaseDeliveryNotes | same | 9 | SUCCESS |
| OPCH | PurchaseInvoices | same | 20 | SUCCESS |
| ORDR | Orders | same | 20 | SUCCESS |
| ODLN | DeliveryNotes | same | 19 | SUCCESS |
| OWTR | StockTransfers | DocEntry,DocNum,DocDate,FromWarehouse,ToWarehouse,UpdateDate | 20 | SUCCESS |

## Objects moved to Prepared

| Object | Reason |
|---|---|
| OITW | `ItemWarehouseInfoCollection` not a top-level SL entity in v1000290 — "Unrecognized resource path" |

## $select fields removed during discovery

The following fields were tested and confirmed invalid in SL v1000290 for purchasing/fulfillment headers:

- `DocTotalSy` — system currency total (not exposed)
- `DocCur` — document currency (not exposed)
- `DocStatus` — document status (not exposed)
- `CreateDate` — creation date (not exposed)

For OWTR additionally:
- `DocTotal` — not exposed
- `Cancelled` — not exposed

## STG tables populated (2026-06-15 transform)

| STG table | Rows after transform |
|---|---|
| stg.purchase_order | 28 |
| stg.purchase_receipt | 28 |
| stg.purchase_invoice | 38 |
| stg.item_warehouse | 0 (OITW in Prepared, no extraction) |
| stg.sales_order | 37 |
| stg.delivery | 35 |
| stg.stock_transfer | 38 |

## MART process-dashboard tables populated

| Table | Rows |
|---|---|
| mart.purchase_executive_daily | 17 |
| mart.purchase_supplier_dashboard | 18 |
| mart.purchase_receiving_dashboard | 10 |
| mart.sales_fulfillment_dashboard | 28 |
| mart.sales_customer_dashboard | 18 |
| mart.sales_item_dashboard | 11 |
| mart.inventory_rotation_dashboard | 41 |
| mart.inventory_stock_dashboard | 0 (OITW not extracted) |
| mart.inventory_warehouse_dashboard | 12 |
| mart.finance_ar_aging_dashboard | 18 |
| mart.finance_ap_aging_dashboard | 0 (OPCH aging TBD) |
| mart.finance_executive_daily | 2 |

## Corrective migrations applied

| Migration | Reason |
|---|---|
| 20260615210100_FixStgRefreshAll | Old STG functions return INT scalar, not TABLE — mixed calling pattern needed |
| 20260615210200_FixMartProcessFunctions | Wrong column names in 3 process functions (card_code vs period_date, wrong PK, wrong column names) |
| 20260615210300_FixInventoryStockGroupBy | Missing GROUP BY in inventory_stock_dashboard INSERT (MAX without GROUP BY) |
