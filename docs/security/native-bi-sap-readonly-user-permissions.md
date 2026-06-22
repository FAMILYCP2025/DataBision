# Native BI Finance — Permisos SAP B1 para Usuario Solo Lectura

**Sprint 27 · DataBision · Junio 2026**

---

## Propósito

Definir los permisos mínimos necesarios para el usuario SAP B1 que usa el extractor DataBision. El principio es **mínimo privilegio**: solo lo estrictamente necesario para leer datos contables.

---

## Nombre sugerido del usuario SAP

```
DATABISION_RO
```

Usar un nombre explícito que identifique el propósito y el acceso de solo lectura. No usar credenciales de usuarios humanos existentes.

---

## Permisos requeridos — MVP Finance

### OACT — Chart of Accounts

| Operación | Permisos requeridos | Método SAP Service Layer |
|---|---|---|
| Leer todas las cuentas | `Read` sobre Business Partners / Chart of Accounts | `GET /ChartOfAccounts` |
| Leer cuenta por código | `Read` sobre Business Partners / Chart of Accounts | `GET /ChartOfAccounts('[Code]')` |

**Objeto SAP:** `ChartOfAccounts`  
**Tabla SAP:** `OACT`

### OJDT — Journal Entry Headers

| Operación | Permisos requeridos | Método SAP Service Layer |
|---|---|---|
| Leer listado de asientos | `Read` sobre Journal Entry | `GET /JournalEntries?$select=...` |
| Leer asiento individual | `Read` sobre Journal Entry | `GET /JournalEntries(N)` |

**Objeto SAP:** `JournalEntries`  
**Tabla SAP:** `OJDT` (headers) + `JDT1` (lines, incluidas automáticamente en GET individual)

---

## Permisos NOT necesarios para Finance MVP

Los siguientes permisos **no deben asignarse** al usuario DataBision:

| Módulo SAP | Razón de exclusión |
|---|---|
| Órdenes de compra (OPOR) | Fuera de scope Finance |
| Facturas de proveedores (OPCH) | Fuera de scope Finance |
| Facturas de clientes (OINV) | Fuera de scope Finance |
| Inventario (OITM, OIBT) | Fuera de scope Finance |
| Cuentas por cobrar / pagar | Fuera de scope Finance |
| Recursos humanos | Fuera de scope en todos los módulos |
| Administración de usuarios SAP | **Nunca — riesgo crítico** |
| Configuración del sistema | **Nunca — riesgo crítico** |

---

## Permisos para módulos futuros (roadmap)

| Módulo futuro | Objeto SAP | Tabla | Permiso adicional requerido |
|---|---|---|---|
| Compras | PurchaseOrders | OPOR, POR1 | Read sobre Purchase Documents |
| Inventario | Items | OITM | Read sobre Items |
| Ventas | SalesOrders | ORDR, RDR1 | Read sobre Sales Documents |
| Notas de crédito | CreditNotes | ORPC | Read sobre Credit Documents |

Estos módulos requieren permisos adicionales que **no se asignan** hasta que el módulo correspondiente esté implementado.

---

## Configuración en SAP Business One (pasos)

### Paso 1: Crear usuario en SAP B1

1. Ir a **Administración → Configurar → Usuarios**
2. Crear nuevo usuario: `DATABISION_RO`
3. Tipo de usuario: **Regular**
4. Contraseña: generada automáticamente, mínimo 12 caracteres, alfanumérica + símbolo
5. Marcar: "El usuario debe cambiar la contraseña al inicio" → **NO** (es un usuario de servicio)

### Paso 2: Asignar permisos mínimos

1. Ir a **Administración → Configurar → Autorizaciones generales**
2. Seleccionar el usuario `DATABISION_RO`
3. Asignar los siguientes permisos:

| Módulo | Formulario | Nivel de permiso |
|---|---|---|
| Finanzas | Plan de Cuentas | Solo lectura |
| Finanzas | Asientos Contables | Solo lectura |
| Todos los demás | — | Sin acceso |

4. Guardar y verificar

### Paso 3: Verificar acceso vía Service Layer

```bash
# Test de acceso — reemplazar valores sin imprimir output
curl -X POST https://[SL_URL]/b1s/v1/Login \
  -H "Content-Type: application/json" \
  -d '{"CompanyDB":"[COMPANY_DB]","UserName":"DATABISION_RO","Password":"[PWD]"}'

# Si retorna SessionId → acceso correcto
# Luego verificar:
curl -H "Cookie: B1SESSION=[SESSION]" \
  "https://[SL_URL]/b1s/v1/ChartOfAccounts?\$top=1"
```

> **Regla:** Nunca imprimir SessionId, Password, ni cookies en logs o terminales visibles.

### Paso 4: Documentar credenciales

Guardar el password en SecretRef:
```
Variable de entorno: SAP_PASSWORD_[NOMBRE_CLIENTE]
Formato SecretRef: env:SAP_PASSWORD_[NOMBRE_CLIENTE]
```

---

## Checklist de validación

- [ ] Usuario `DATABISION_RO` creado en SAP B1
- [ ] Permisos limitados a ChartOfAccounts y JournalEntries (solo lectura)
- [ ] Sin permisos de escritura en ningún módulo
- [ ] Password almacenado como SecretRef (nunca en texto plano)
- [ ] Test de conexión exitoso desde extractor DataBision
- [ ] Test de GET `/ChartOfAccounts` retorna datos correctos
- [ ] Test de GET `/JournalEntries` retorna datos correctos
- [ ] Usuario no puede crear ni modificar ningún objeto SAP
- [ ] SAP Basis tiene registro del usuario y su propósito

---

## Notas para SAP Basis

El usuario `DATABISION_RO` es un usuario de servicio para el extractor DataBision. Específicamente:

- Se conecta únicamente vía SAP Service Layer (REST API)
- Realiza únicamente peticiones GET
- Opera en horario programado (normalmente 2am–4am)
- Si el usuario realiza peticiones POST o PUT — alerta de seguridad

Si SAP Basis necesita auditar el acceso, puede revisar los logs del Service Layer filtrando por `DATABISION_RO`.
