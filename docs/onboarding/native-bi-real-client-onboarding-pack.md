# Native BI Finance — Pack de Onboarding para Cliente Real

**Sprint 29 · DataBision · Junio 2026**  
**Versión: 1.0 — Para primer piloto pagado**

---

## Propósito

Este documento es el checklist maestro de onboarding. Define qué información necesita DataBision del cliente, qué hace cada parte, y qué valida antes de iniciar la implementación.

---

## Sección 1 — Información general del cliente

| Campo | Valor | Responsable |
|---|---|---|
| Razón social | _________________________ | Cliente |
| RUC / NIT | _________________________ | Cliente |
| Nombre comercial | _________________________ | Cliente |
| Sector / industria | _________________________ | Cliente |
| País de operación | _________________________ | Cliente |
| Moneda funcional | PEN / USD / otro: _______ | Cliente |
| Plan contable | PCGE / NIIF / otro: ______ | Cliente |
| Versión SAP B1 | _________________________ | TI Cliente |
| Tipo SAP B1 | On-premise / HANA Cloud | TI Cliente |

---

## Sección 2 — Acceso SAP Business One

| Campo | Valor | Responsable |
|---|---|---|
| URL SAP Service Layer | `https://[host]:[puerto]/b1s/v1` | TI Cliente |
| CompanyDB (base de datos) | _________________________ | TI Cliente / SAP Basis |
| Usuario SAP dedicado | `DATABISION_RO` (sugerido) | SAP Basis |
| Certificado SSL en Service Layer | Válido / Autofirmado / Ninguno | TI Cliente |
| Ambiente a conectar | TST / PRD | Cliente + DataBision |
| IP del servidor SAP | _________________________ | TI Cliente |
| Puerto Service Layer | 50000 (default) / otro: ____ | TI Cliente |

> **Regla:** Iniciar siempre en TST si existe. Nunca en PRD como primer ambiente.

---

## Sección 3 — Usuario SAP solo lectura

El cliente debe crear un usuario SAP con las siguientes características:

| Atributo | Valor |
|---|---|
| Username | `DATABISION_RO` (o el que defina SAP Basis) |
| Tipo | Regular |
| Permisos | Solo lectura sobre OACT y OJDT |
| Sin permisos | Crear/modificar documentos, administración, usuarios |
| Password | Mínimo 12 caracteres, alfanumérico + símbolo |

Referencia: [native-bi-sap-readonly-user-permissions.md](../security/native-bi-sap-readonly-user-permissions.md)

---

## Sección 4 — Certificado SSL

| Situación | Acción |
|---|---|
| SSL válido (CA reconocida) | Listo — conectar directamente |
| SSL autofirmado en TST | Permitido en TST con `IgnoreSslErrors=true` en perfil TST únicamente |
| Sin SSL en PRD | **No proceder** — solicitar certificado antes de conectar a PRD |
| Certificado expirado | Solicitar renovación a TI antes de iniciar |

---

## Sección 5 — Responsables del proyecto

| Rol | Nombre | Email | Teléfono |
|---|---|---|---|
| **Responsable financiero** (CFO / Gerente) | _____________ | _____________ | _____________ |
| **Responsable TI** (acceso SAP, firewall) | _____________ | _____________ | _____________ |
| **SAP Basis / Administrador SAP** | _____________ | _____________ | _____________ |
| **Contador / Jefe de contabilidad** | _____________ | _____________ | _____________ |
| **Consultor DataBision** | Jonathan Campillay | campillayparedes@gmail.com | — |

---

## Sección 6 — Período contable a validar

| Campo | Valor |
|---|---|
| Período de validación del piloto | _________________________ (ej: Enero–Diciembre 2025) |
| Mes de referencia para P&L | _________________________ (ej: Diciembre 2025) |
| Fecha de cierre contable último | _________________________ |
| ¿Hay asientos de cierre pendientes? | Sí / No |
| ¿Se permiten asientos retrospectivos en SAP? | Sí / No |

---

## Sección 7 — Plan de cuentas

| Campo | Valor |
|---|---|
| Número aproximado de cuentas OACT | _________________________ |
| ¿Hay cuentas auxiliares o de control? | Sí / No |
| ¿Existe clasificación previa por el contador? | Sí / No / Parcial |
| Categorías PCGE utilizadas | 10–99 / Solo 60–79 / otra: ________ |
| ¿Hay cuentas de orden o cuentas analíticas? | Sí / No |

---

## Sección 8 — Horario de extracción

| Campo | Valor |
|---|---|
| Horario de mantenimiento SAP | _________________________ |
| Horario sin usuarios activos | _________________________ (ej: 1am–5am) |
| Horario sugerido OJDT diario | 02:00 AM (default) / otro: ______ |
| Horario sugerido OACT semanal | Lunes 01:00 AM / otro: ______ |
| Horario sugerido MART | 02:30 AM / otro: ______ |
| ¿SAP tiene ventana de mantenimiento nocturno? | Sí: _______ / No |
| Zona horaria del servidor SAP | _________________________ (ej: America/Lima) |

---

## Sección 9 — Firewall y Red

| Requisito | Estado |
|---|---|
| El servidor del extractor puede conectar a SAP SL en puerto [PUERTO] | ☐ Confirmado / ☐ Pendiente |
| El servidor del extractor puede conectar a DataBision API (HTTPS 443) | ☐ Confirmado / ☐ Pendiente |
| No hay proxy intermedio que intercepte SSL | ☐ Confirmado / ☐ Verificar |
| Whitelist de IP del extractor en SAP (si aplica) | ☐ Confirmado / ☐ No aplica |

---

## Checklist de inicio (Go/No-Go pre-implementación)

- [ ] URL Service Layer proporcionada y verificada
- [ ] CompanyDB proporcionada
- [ ] Usuario SAP `DATABISION_RO` creado con permisos correctos
- [ ] Password SAP almacenado como SecretRef (no en texto plano)
- [ ] Certificado SSL evaluado (válido en PRD o TST explícitamente etiquetado)
- [ ] Firewall abierto para extractor → SAP Service Layer
- [ ] Firewall abierto para extractor → DataBision API
- [ ] Responsable financiero confirmado para sesión de validación
- [ ] Contador confirmado para sesión de clasificación (2 horas)
- [ ] Período contable definido para el piloto
- [ ] Propuesta firmada y pago confirmado

Solo proceder con la implementación cuando todos los ítems estén marcados.
