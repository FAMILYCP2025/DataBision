# DataBision Native BI Finance — Checklist de Onboarding Cliente SAP B1

**Versión:** 1.0  
**Sprint:** 21A  
**Fecha:** 2026-06-20  
**Audiencia:** Consultor DataBision + Responsable TI del cliente

---

## 1. Información requerida del cliente SAP B1

Solicitar antes de iniciar cualquier configuración técnica.

### 1.1 Base de datos SAP

| Campo | Descripción | Ejemplo |
|---|---|---|
| `CompanyDB` | Nombre de la base de datos SAP | `CLTSTKSDEPOR` |
| `Versión SAP B1` | SAP B1 9.3, 10.0, 10.x | `10.0 PL08` |
| `Versión Service Layer` | Visible en `/b1s/v1/$metadata` | `v1000290` |
| `URL Service Layer` | IP o FQDN + puerto | `https://192.168.1.10:50000` |
| `Usuario SL` | Usuario con permisos de lectura | `DataBisionReader` |
| `Password SL` | *(nunca por email — usar 1Password o entrega cifrada)* | — |
| `Zona horaria SAP` | Para interpretar timestamps | `America/Lima` |
| `Moneda base` | Moneda funcional del libro diario | `PEN` |
| `País / Plan contable` | Para seleccionar reglas PCGE correctas | `PE / PCGE` |
| `Motor de base de datos` | HANA o SQL Server | `HANA` |

> **Requerimiento mínimo:** SAP B1 versión 9.3+ con Service Layer habilitado. HANA o SQL Server con SL v1000290+.

### 1.2 Objetos SAP requeridos

DataBision lee los siguientes objetos vía Service Layer (solo lectura):

| Objeto | Endpoint SL | Módulo |
|---|---|---|
| `OACT` | `ChartOfAccounts` | Finanzas |
| `OJDT` | `JournalEntries` | Finanzas |
| `JDT1` | `JournalEntries(N)` — inline en GET individual | Finanzas |
| `OCRD` | `BusinessPartners` | Ventas / Compras |
| `OITM` | `Items` | Inventario |
| `OSLP` | `SalesPersons` | Ventas |
| `OINV` | `Invoices` | Ventas |
| `ORIN` | `CreditNotes` | Ventas |
| `OPOR` | `PurchaseOrders` | Compras |
| `OPDN` | `PurchaseDeliveryNotes` | Compras |
| `OPCH` | `PurchaseInvoices` | Compras |
| `ORDR` | `Orders` | Ventas |
| `ODLN` | `DeliveryNotes` | Ventas |
| `OWTR` | `StockTransfers` | Inventario |

**Nota:** DataBision no escribe, no modifica, no borra datos SAP. Solo lectura.

### 1.3 Permisos mínimos Service Layer

Crear usuario dedicado SAP B1 con los siguientes permisos (solo lectura):

```
Módulo General Ledger      → Ver
Módulo Business Partners   → Ver
Módulo Inventory           → Ver
Módulo Sales               → Ver
Módulo Purchasing          → Ver
```

El usuario debe poder ejecutar `POST /b1s/v1/Login` y `GET` en los endpoints listados. No requiere permisos de escritura, aprobación ni administración.

---

## 2. Checklist de conectividad

Validar antes de configurar el extractor:

- [ ] Puerto SAP Service Layer accesible: `https://[HOST]:50000/b1s/v1/$metadata` retorna 200
- [ ] Si puerto 50001 (HTTPS-only): validar certificado o habilitar `IgnoreSslCertificateErrors`
- [ ] Firewall permite tráfico TCP desde el servidor DataBision Extractor al servidor SAP
- [ ] DNS resuelve el host SAP (o usar IP directa si no hay DNS interno)
- [ ] Login manual válido: `POST /b1s/v1/Login` con `{ "CompanyDB": "X", "UserName": "Y", "Password": "Z" }` retorna 200
- [ ] `GET /b1s/v1/ChartOfAccounts?$top=5` retorna filas (confirma permisos read)
- [ ] `GET /b1s/v1/JournalEntries?$top=1` retorna al menos 1 asiento
- [ ] Sin restricción de IP en el usuario SAP (algunos clientes tienen IP whitelist en SL)

---

## 3. Checklist configuración DataBision

### 3.1 Setup en AppDB (panel SuperAdmin)

- [ ] Crear empresa en SuperAdmin: nombre, slug, logo, colores
- [ ] Configurar `AnalyticsCompanyId` (ej: `company-clienteabc-001`) — debe ser único
- [ ] Asignar módulos habilitados: mínimo `native_bi_finance`
- [ ] Configurar branding: colores primarios, logo

### 3.2 Setup extractor (servidor DataBision)

- [ ] Copiar `appsettings.Development.template.json` a `appsettings.[Env].json`
- [ ] Completar todos los campos:
  ```json
  {
    "SapServiceLayer": {
      "BaseUrl": "https://[HOST]:50000/b1s/v1",
      "CompanyDB": "[COMPANYDB]",
      "UserName": "[USER]",
      "Password": "[PASSWORD]",
      "IgnoreSslCertificateErrors": false,
      "TimeoutSeconds": 60
    },
    "DataBisionApi": {
      "BaseUrl": "https://api.databision.app",
      "ApiKey": "[INGEST_API_KEY]"
    },
    "Staging": {
      "ConnectionString": "[SUPABASE_CONNECTION_STRING]"
    },
    "Extractor": {
      "TenantId": "[TENANT_ID]",
      "CompanyId": "[ANALYTICS_COMPANY_ID]",
      "Mode": "INCREMENTAL",
      "PageSize": 100,
      "LookbackMinutes": 10,
      "JournalEntryLineFetchConcurrency": 3
    }
  }
  ```
- [ ] Ejecutar `dotnet run -- --dry-run` → confirma config OK
- [ ] Ejecutar `dotnet run -- --validate` → confirma login SAP OK

### 3.3 Configuración reglas PCGE (clasificación contable)

- [ ] Abrir SuperAdmin → Empresa → Native BI → Clasificación de Cuentas
- [ ] Importar reglas base PCGE para el país del cliente (PE/PCGE, CO/PUC, CL/IFRS, etc.)
- [ ] Ejecutar "Sugerencias desde OACT" → revisar con el contador del cliente
- [ ] Ajustar reglas específicas para cuentas no estándar del cliente
- [ ] Validar que `cfg.account_classification_rules` tiene ≥ 50 reglas

### 3.4 Primera extracción

- [ ] OACT (full refresh): `dotnet run -- --object OACT --send`
- [ ] OJDT (incremental): `dotnet run -- --object OJDT --send`
- [ ] Verificar en Supabase: `raw.sap_oact` y `raw.sap_ojdt` + `raw.sap_jdt1` con filas
- [ ] Ejecutar pipeline: `dotnet run -- --transform-mart --company [ANALYTICS_COMPANY_ID]`
- [ ] Verificar: `SELECT * FROM mart.refresh_accounting_all('[ANALYTICS_COMPANY_ID]')` → 8 filas OK

### 3.5 Validación readiness

- [ ] `GET /api/client/bi/finance/readiness?companyId=[SLUG]` → `readinessStatus = "ready"`
- [ ] `GET /api/client/bi/finance/validations?companyId=[SLUG]` → `healthScore ≥ 80`
- [ ] Los 6 endpoints financieros retornan HTTP 200
- [ ] Dashboard Finance accesible en `https://[SLUG].databision.app`

---

## 4. Criterios Go/No-Go para primer acceso del cliente

| Criterio | Mínimo aceptable | Go / No-Go |
|---|---|---|
| Service Layer conectividad | Login exitoso | ✅ / ❌ |
| OACT extraído | ≥ 10 cuentas | ✅ / ❌ |
| OJDT extraído | ≥ 1 asiento | ✅ / ❌ |
| refresh_accounting_all | 8/8 OK | ✅ / ❌ |
| Cuentas clasificadas | ≥ 80% | ✅ / ❌ |
| readinessStatus | "ready" o "warning" | ✅ / ❌ |
| healthScore | ≥ 70 | ✅ / ❌ |
| Endpoint readiness HTTP 200 | ✅ | ✅ / ❌ |
| Income statement con datos | Al menos 1 período | ✅ / ❌ |

**No-Go automático si:**
- No hay conectividad SAP SL (firewall / credenciales)
- refresh_accounting_all falla en algún paso
- 0 cuentas clasificadas (sin reglas PCGE configuradas)
- healthScore < 50

---

## 5. Notas importantes

### Limitaciones conocidas (a comunicar al cliente)

1. **DataBision NO modifica datos SAP.** Es solo lectura.
2. **DataBision NO reemplaza SAP FI.** Es un módulo de reportería.
3. **Los reportes NO son auditoría contable.** Requieren validación del contador.
4. **El balance puede no cuadrar** si SAP no tiene asientos de cierre de ejercicio.
5. **La clasificación PCGE es automática pero revisable.** El cliente puede ajustar reglas.
6. **Datos de TST/prueba** son distintos a datos de producción — siempre usar producción para demos finales.

### SAP Business One — nombres de base de datos

Los clientes SAP suelen tener varias bases de datos:
- `COMPANY_PROD` — producción
- `COMPANY_TST` — ambiente de prueba
- `COMPANY_DEV` — desarrollo

Siempre confirmar cuál usar. DataBision extrae de una sola base por perfil de configuración.
