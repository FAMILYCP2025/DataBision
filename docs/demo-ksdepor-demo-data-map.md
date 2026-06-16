# Demo KSDEPOR — Data Map

Sprint 8P — Junio 2026

Mapeo completo de pantalla → KPI → tabla MART → objeto SAP → mensaje comercial. Referencia técnica para el equipo de ventas y para responder preguntas técnicas durante la demo.

---

## Módulo: Ventas

### KPI Cards

| KPI visible en UI | Campo MART | Tabla MART | Objeto SAP origen | Mensaje comercial |
|---|---|---|---|---|
| Ventas netas | `net_sales_amount` | `mart.bi_sales_daily_summary` | OINV (facturas de venta) | Ingresos reales descontando devoluciones |
| Ventas brutas | `gross_sales_amount` | `mart.bi_sales_daily_summary` | OINV | Ingresos totales antes de descuentos |
| Facturas emitidas | `invoice_count` | `mart.bi_sales_daily_summary` | OINV | Volumen de operaciones facturadas |
| Ticket promedio | `avg_ticket_amount` | `mart.bi_sales_daily_summary` | OINV | Tamaño promedio de operación |

### Tab Clientes

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Cliente (nombre) | `card_name` | `mart.bi_sales_by_customer` | OINV → OCRD | Identificación clara del cliente |
| Ventas netas | `net_sales_amount` | `mart.bi_sales_by_customer` | OINV | Ranking de clientes por valor |
| Facturas | `invoice_count` | `mart.bi_sales_by_customer` | OINV | Frecuencia de compra |
| Ticket prom. | `avg_ticket_amount` | `mart.bi_sales_by_customer` | OINV | Potencial de ticket por cliente |
| Última factura | `last_invoice_date` | `mart.bi_sales_by_customer` | OINV | Clientes inactivos (sin compras recientes) |

### Tab Productos

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Producto | `item_name` | `mart.bi_sales_by_item` | OINV lines → OITM | SKU que genera más ingresos |
| Cantidad | `quantity_sold` | `mart.bi_sales_by_item` | OINV lines | Volumen de unidades vendidas |
| Ventas brutas | `gross_sales_amount` | `mart.bi_sales_by_item` | OINV lines | Producto más rentable por monto |
| Última venta | `last_sale_date` | `mart.bi_sales_by_item` | OINV lines | SKU sin movimiento reciente |

### Tab Vendedores

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Vendedor | `sales_person_name` | `mart.bi_sales_by_salesperson` | OINV → OSLP | Desempeño individual del equipo |
| Ventas netas | `net_sales_amount` | `mart.bi_sales_by_salesperson` | OINV | Ranking de vendedores por ingresos |
| Clientes | `active_customers` | `mart.bi_sales_by_salesperson` | OINV | Cartera de clientes activa por vendedor |
| Ticket prom. | `avg_ticket_amount` | `mart.bi_sales_by_salesperson` | OINV | Calidad de las operaciones por vendedor |

### Tab Fulfillment

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Fecha | `period_date` | `mart.bi_sales_fulfillment` | ORDR + ODLN | Período de análisis |
| Pedidos | `orders_count` | `mart.bi_sales_fulfillment` | ORDR | Demanda recibida |
| Monto pedidos | `orders_amount` | `mart.bi_sales_fulfillment` | ORDR | Valor en riesgo de no entregar |
| Entregas | `delivered_count` | `mart.bi_sales_fulfillment` | ODLN | Cumplimiento efectivo |
| Tasa cumplimiento | `fill_rate_pct` | `mart.bi_sales_fulfillment` | ORDR/ODLN | Eficiencia logística (< 80% = alerta) |
| Pendientes | `pending_orders` | `mart.bi_sales_fulfillment` | ORDR sin ODLN | Pedidos sin entregar |

---

## Módulo: Compras

### KPI Cards

| KPI visible en UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Órdenes de compra | `po_count` | `mart.bi_purchasing_daily_summary` | OPOR | Volumen de compras emitidas |
| Monto OC | `po_amount` | `mart.bi_purchasing_daily_summary` | OPOR | Gasto comprometido con proveedores |
| Monto recibido | `received_amount` | `mart.bi_purchasing_daily_summary` | OPDN | Mercadería efectivamente ingresada |
| Proveedores activos | `active_suppliers` | `mart.bi_purchasing_daily_summary` | OPOR → OCRD | Diversificación de proveedores |

### Tab Proveedores

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Proveedor | `supplier_name` | `mart.bi_purchasing_by_supplier` | OCRD | Identificación del proveedor clave |
| OC | `po_count` | `mart.bi_purchasing_by_supplier` | OPOR | Frecuencia de órdenes |
| Monto OC | `po_amount` | `mart.bi_purchasing_by_supplier` | OPOR | Concentración del gasto |
| Recibido | `received_amount` | `mart.bi_purchasing_by_supplier` | OPDN | Cumplimiento del proveedor |
| Última OC | `last_po_date` | `mart.bi_purchasing_by_supplier` | OPOR | Proveedores inactivos |

### Tab Recepciones

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Proveedor | `supplier_name` | `mart.bi_purchasing_receiving` | OPDN → OCRD | Trazabilidad de recepciones |
| Recepciones | `gr_count` | `mart.bi_purchasing_receiving` | OPDN | Volumen de ingresos de mercadería |
| Monto recibido | `gr_amount` | `mart.bi_purchasing_receiving` | OPDN | Valor ingresado a bodega |
| Última recepción | `last_gr_date` | `mart.bi_purchasing_receiving` | OPDN | Control de entregas recientes |

---

## Módulo: Inventario

### KPI Cards

| KPI visible en UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Alta rotación | Conteo `rotation_status='FAST'` | `mart.bi_inventory_rotation` | OINV lines → OITM | SKUs que se venden constantemente |
| Rotación normal | Conteo `rotation_status='NORMAL'` | `mart.bi_inventory_rotation` | OINV lines → OITM | SKUs con movimiento estable |
| Baja rotación | Conteo `rotation_status='SLOW'` | `mart.bi_inventory_rotation` | OINV lines → OITM | SKUs con señal de sobrestock |
| Sin movimiento | Conteo `rotation_status='NO_MOVEMENT'` | `mart.bi_inventory_rotation` | OINV lines → OITM | Capital inmovilizado (90d sin ventas) |

### Tab Rotación

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Artículo | `item_name` | `mart.bi_inventory_rotation` | OITM | SKU con clasificación de rotación |
| Rotación (badge) | `rotation_status` | `mart.bi_inventory_rotation` | OITM + OINV | Categoría: FAST/NORMAL/SLOW/NO_MOVEMENT |
| Cant. 30d | `qty_sold_30d` | `mart.bi_inventory_rotation` | OINV lines | Demanda reciente del SKU |
| Cant. 90d | `qty_sold_90d` | `mart.bi_inventory_rotation` | OINV lines | Demanda de mediano plazo |
| Cobertura días | `coverage_days` | `mart.bi_inventory_rotation` | OITM + OINV | Días de stock disponible (reorden) |
| Última venta | `last_sale_date` | `mart.bi_inventory_rotation` | OINV lines | SKU sin ventas recientes |

### Tab Almacenes

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Almacén | `warehouse_name` | `mart.bi_inventory_warehouses` | OWTR → OWHS | Movimiento por ubicación física |
| Entradas | `transfer_in_count` | `mart.bi_inventory_warehouses` | OWTR | Ingresos de mercadería al almacén |
| Cant. entrada | `transfer_in_qty` | `mart.bi_inventory_warehouses` | OWTR | Volumen de ingresos |
| Salidas | `transfer_out_count` | `mart.bi_inventory_warehouses` | OWTR | Egresos del almacén |
| Último traslado | `last_transfer_date` | `mart.bi_inventory_warehouses` | OWTR | Almacenes sin actividad reciente |

---

## Módulo: Finanzas

### KPI Cards

| KPI visible en UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| AR vencido | `ar_overdue` | `mart.bi_finance_daily_summary` | OINV | Deuda vencida de clientes |
| % vencido (período) | `ar_overdue_pct` | `mart.bi_finance_daily_summary` | OINV | Proporción de cartera en mora |
| Facturas emitidas (30d) | `new_invoices_count` | `mart.bi_finance_daily_summary` | OINV | Actividad de facturación |
| Monto facturado (30d) | `new_invoices_amount` | `mart.bi_finance_daily_summary` | OINV | Ingresos del período |

### Tab AR (Cuentas por cobrar)

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Cliente | `card_name` | `mart.bi_finance_ar_aging` | OCRD | Cliente con saldo pendiente |
| Saldo | `balance_due` | `mart.bi_finance_ar_aging` | OINV | Deuda total vigente |
| Vencido (rojo) | `overdue_amount` | `mart.bi_finance_ar_aging` | OINV | Deuda fuera de plazo |
| 0-30d | `aging_0_to_30` | `mart.bi_finance_ar_aging` | OINV | Deuda reciente, menor riesgo |
| 31-60d | `aging_31_to_60` | `mart.bi_finance_ar_aging` | OINV | Deuda en zona de seguimiento |
| +90d (rojo) | `aging_90_plus` | `mart.bi_finance_ar_aging` | OINV | Deuda de difícil cobro |
| Últ. factura | `last_invoice_date` | `mart.bi_finance_ar_aging` | OINV | Actividad reciente del cliente |

### Tab AP (Cuentas por pagar)

| Columna UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Proveedor | `supplier_name` | `mart.bi_finance_ap_aging` | OCRD | Proveedor con saldo pendiente |
| Saldo | `balance_due` | `mart.bi_finance_ap_aging` | OPCH | Deuda total con el proveedor |
| Vencido | `overdue_amount` | `mart.bi_finance_ap_aging` | OPCH | Pago atrasado (riesgo de relación) |
| +90d | `aging_90_plus` | `mart.bi_finance_ap_aging` | OPCH | Deuda con proveedor críticamente vencida |

---

## Módulo: Operaciones

### KPI Cards

| KPI visible en UI | Campo MART | Tabla MART | Objeto SAP | Mensaje comercial |
|---|---|---|---|---|
| Health score | `health_score` | `ops.pipeline_health` | N/A (calculado) | Estado global del pipeline 0-100 |
| Alertas activas | `active_alerts` | `ops.pipeline_health` | N/A | Problemas pendientes de resolución |
| Objetos extraídos | `objects_extracted` | `ops.pipeline_health` | `ctl.extractor_run` | Cobertura del sistema de extracción |
| Errores DQ sin resolver | `dq_errors_unresolved` | `ops.pipeline_health` | `ops.data_quality` | Calidad de datos pendiente |

### Tab Alertas

| Columna UI | Campo | Tabla | Mensaje comercial |
|---|---|---|---|
| Severidad (badge) | `severity` | `ops.alert_event` | Prioridad del problema: critical/warning/info |
| Regla | `rule_code` | `ops.alert_event` | Identificador técnico de la regla |
| Mensaje | `message` | `ops.alert_event` | Descripción en lenguaje natural |
| Valor | `triggered_value` | `ops.alert_event` | El dato que disparó la alerta |
| Disparada | `triggered_at_utc` | `ops.alert_event` | Cuándo se detectó el problema |

### Tab Calidad de datos

| Columna UI | Campo | Tabla | Mensaje comercial |
|---|---|---|---|
| Objeto SAP | `sap_object` | `ops.data_quality` | Qué tabla de SAP tiene el problema |
| Tipo | `issue_type` | `ops.data_quality` | Categoría del problema de calidad |
| Severidad | `severity` | `ops.data_quality` | Impacto en los análisis |
| Descripción | `description` | `ops.data_quality` | Explicación del problema |
| Filas afectadas | `affected_rows` | `ops.data_quality` | Escala del problema |

---

## Reglas de alerta (`ops.alert_rule`)

| Código de regla | Condición | Severidad | Mensaje comercial |
|---|---|---|---|
| `STALE_EXTRACTOR` | Último run > 26h | critical | El extractor no corrió ayer |
| `HIGH_ERROR_RATE` | >20% de runs en error | warning | Alta tasa de errores en extracción |
| `LOW_FILL_RATE` | Fill rate < 70% | warning | Cumplimiento de pedidos bajo umbral |
| `HIGH_AR_OVERDUE_PCT` | AR vencido > 30% | warning | Más del 30% de cartera vencida |
| `ZERO_SALES_3D` | Sin ventas 3 días | warning | 3 días sin registrar ventas |
| `NO_MOVEMENT_SKU_EXCESS` | >50% SKUs sin movimiento | info | Exceso de inventario sin rotación |
| `AP_OVERDUE_CRITICAL` | AP vencido > 90d | critical | Pagos a proveedores críticamente atrasados |
| `DQ_CRITICAL_ISSUES` | DQ crítica sin resolver | critical | Errores de calidad de datos bloqueantes |

---

## Legenda de objetos SAP

| Código SAP | Descripción | Módulo DataBision |
|---|---|---|
| OINV | Facturas de venta | Ventas, Finanzas |
| ORDR | Órdenes de venta | Ventas (Fulfillment) |
| ODLN | Entregas / notas de entrega | Ventas (Fulfillment) |
| OPOR | Órdenes de compra | Compras |
| OPDN | Recepciones de compra | Compras |
| OPCH | Facturas de proveedor | Finanzas (AP) |
| OITM | Maestro de artículos | Inventario, Ventas |
| OWTR | Transferencias de stock | Inventario (Almacenes) |
| OCRD | Business Partners (clientes/proveedores) | Ventas, Compras, Finanzas |
| OSLP | Maestro de vendedores | Ventas |
| OWHS | Maestro de almacenes | Inventario |
