# DataBision Native BI Finance — Alcance del Piloto (Scope Document)

**Sprint:** 21F  
**Fecha:** 2026-06-20  
**Uso:** Documento de alcance a incluir en propuesta comercial

---

## Alcance IN (incluido en el piloto)

### Extracción de datos (solo lectura desde SAP B1)

| Objeto SAP | Qué extrae | Para qué módulo |
|---|---|---|
| OACT | Plan de cuentas | Finanzas |
| OJDT | Cabecera del libro diario | Finanzas |
| JDT1 | Líneas del libro diario (vía JournalEntries(N)) | Finanzas |

### Dashboard financiero (módulo Native BI Finance)

| Reporte | Descripción |
|---|---|
| Estado de Resultados | P&L por período (mensual), clasificado por PCGE |
| Balance General | Activos, pasivos y patrimonio por snapshot |
| EBITDA | Tendencia mensual con gross profit, financial result |
| Plan de Cuentas | Catálogo completo con clasificación y saldos |
| Validaciones | Health score, issues, readiness check |

### Funcionalidades incluidas

- Export CSV de todos los reportes (para Excel, Power BI, etc.)
- Clasificación PCGE automática por prefijo de cuenta
- Ajuste manual de clasificaciones vía panel de administración
- Acceso web en `https://[slug].databision.app`
- Actualización programada (semanal en Opción B, personalizable en Opción C)

---

## Alcance OUT (NO incluido en el piloto)

### Módulos no incluidos en Opción A y B

| Módulo | Descripción |
|---|---|
| Ventas | Dashboard de facturación, AR aging, crédito |
| Inventario | Kardex, rotación, stock por almacén |
| Compras | AP aging, órdenes de compra, proveedores |
| Comercial | Dashboard de vendedores, pipeline |

*Estos módulos pueden cotizarse por separado o incluirse en Opción C.*

### Funcionalidades explícitamente fuera de alcance

1. **Modificación de datos SAP:** DataBision NO escribe en SAP. Solo lectura.
2. **Reemplazo de contabilidad oficial:** DataBision es reportería auxiliar. El cliente mantiene su contabilidad en SAP.
3. **Auditoría contable:** Los reportes no tienen valor legal de auditoría.
4. **Soporte SAP:** DataBision no presta soporte al sistema SAP del cliente.
5. **Desarrollo a medida:** El piloto usa el módulo estándar. No se agregan reportes o métricas personalizadas.
6. **Integración ERP:** No se integra con otros sistemas del cliente (WMS, CRM, etc.).
7. **Múltiples bases SAP:** El piloto cubre una base de datos SAP (`CompanyDB`). Multi-base requiere Opción C o cotización especial.

---

## Prerequisitos del cliente (obligatorios para comenzar)

| Prerequisito | Detalle |
|---|---|
| SAP Business One activo | Versión 9.3 o superior con Service Layer habilitado |
| Acceso Service Layer | URL, puerto 50000/50001 accesible, usuario y password |
| Usuario SAP read-only | Usuario con permisos de solo lectura en los módulos requeridos |
| Contador disponible | 2 horas en el Día 2 para validar clasificación PCGE |
| Conectividad | Servidor DataBision puede alcanzar servidor SAP (firewall, VPN) |

Si algún prerequisito no está cumplido al inicio del trabajo técnico, los días del piloto se pautan desde que el prerequisito esté disponible.

---

## Criterios de éxito del piloto (Opción B)

Al finalizar el piloto, el cliente debe poder:

1. Acceder al dashboard en `https://[slug].databision.app`
2. Ver el Estado de Resultados del mes actual con datos reales de SAP
3. Exportar el ES a CSV sin necesidad de Excel intermedio
4. Identificar el EBITDA mensual en menos de 30 segundos
5. Tener ≥ 80% de cuentas SAP clasificadas correctamente según PCGE

Si alguno de estos criterios no se cumple, DataBision trabajará sin costo adicional hasta cumplirlos o devolverá el monto pagado.

---

## Confidencialidad y datos

- DataBision accede a los datos financieros del cliente bajo estricta confidencialidad.
- Los datos se almacenan en Supabase (cloud de confianza, región configurable).
- DataBision nunca comparte datos del cliente con terceros.
- Las credenciales SAP del cliente nunca se almacenan en texto plano ni se transmiten por canales no cifrados.
- Al finalizar el contrato, los datos del cliente se eliminan según la política de retención acordada.
