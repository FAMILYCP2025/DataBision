# Native BI Finance — Checklist Hardening Productivo Mínimo

**Sprint 27 · DataBision · Junio 2026**  
**Aplicar antes de onboarding de primer cliente real**

---

## 1. HTTPS y Transporte

| # | Control | Estado | Notas |
|---|---|---|---|
| 1.1 | API DataBision expuesta únicamente via HTTPS | ☐ | Certificado válido, no autofirmado |
| 1.2 | HTTP redirige a HTTPS (301 permanente) | ☐ | Configurado en reverse proxy |
| 1.3 | HSTS habilitado (`Strict-Transport-Security`) | ☐ | min-age: 31536000 |
| 1.4 | SSL válido para SAP Service Layer del cliente | ☐ | Verificar con `curl -I https://[SL_URL]` |
| 1.5 | `IgnoreSslErrors = false` en todos los perfiles de producción | ☐ | **Crítico: NUNCA true en producción** |
| 1.6 | `IgnoreSslErrors` bloqueado cuando `ASPNETCORE_ENVIRONMENT=Production` | ☐ | Validación defensiva en código o doc |

> **Nota:** `IgnoreSslErrors=true` está permitido SOLO en perfiles DEV/TST explícitamente etiquetados. En el código, si `ASPNETCORE_ENVIRONMENT=Production` y `IgnoreSslErrors=true`, la conexión debe rechazarse con error claro.

---

## 2. Credenciales y Secrets

| # | Control | Estado | Notas |
|---|---|---|---|
| 2.1 | Password SAP almacenado como `env:SAP_PASSWORD_[CLIENT]` | ☐ | Nunca en appsettings.json |
| 2.2 | Variables de entorno cargadas desde archivo `.env` no versionado | ☐ | `.env` en `.gitignore` |
| 2.3 | API Key de ingest por cliente (`Ingest__ApiKeys__CLIENT_KEY`) | ☐ | Una key por cliente, nunca compartida |
| 2.4 | JWT private key cargada desde variable de entorno | ☐ | Nunca en repositorio |
| 2.5 | Connection string cargado desde variable de entorno | ☐ | Nunca hardcodeado |
| 2.6 | Rotation de API key documentada y posible sin downtime | ☐ | Procedure en runbook |
| 2.7 | Azure Key Vault configurado como objetivo futuro | ☐ | Usar `azure-kv://` prefix cuando esté disponible |
| 2.8 | Ningún secreto aparece en logs de la aplicación | ☐ | Revisar con `grep -r "password\|secret\|key" logs/` |

### Niveles de SecretRef aceptables

| Nivel | Formato | Aceptable en producción |
|---|---|---|
| Mínimo | `env:VARIABLE_NAME` | ✅ Sí — para piloto |
| Recomendado | `azure-kv://vault/secret` | ✅ Sí — para cliente Enterprise |
| No aceptable | Valor literal en config | ❌ Nunca |
| No aceptable | Hardcodeado en código | ❌ Nunca |

---

## 3. Usuario SAP solo lectura

| # | Control | Estado | Notas |
|---|---|---|---|
| 3.1 | Usuario SAP dedicado para DataBision (no compartido) | ☐ | Nombre sugerido: `DATABISION_RO` |
| 3.2 | Permisos únicamente sobre OACT y OJDT | ☐ | Sin acceso a documentos operativos |
| 3.3 | Sin permisos de creación o modificación | ☐ | Solo `GET` via Service Layer |
| 3.4 | Password del usuario SAP documentado en SecretRef | ☐ | No en texto plano en ningún lugar |
| 3.5 | Usuario SAP bloqueado si se detecta uso fuera del extractor | ☐ | Alertar a SAP Basis |

---

## 4. Isolación por cliente

| # | Control | Estado | Notas |
|---|---|---|---|
| 4.1 | `company_id` en cada query a tablas de datos | ☐ | Validar en DataBision.Infrastructure |
| 4.2 | API Key de ingest valida `company_id` del payload | ☐ | Rechazar si no coincide |
| 4.3 | JWT claims incluyen `company_id` correcto | ☐ | Validado en TenantMiddleware |
| 4.4 | Perfil de conexión SAP usa `company_id` del cliente | ☐ | NativeBiConnectionProfile.CompanyId |
| 4.5 | Dashboard nunca muestra datos de otro tenant | ☐ | Test de aislamiento por company_id |

---

## 5. Logs y Auditoría

| # | Control | Estado | Notas |
|---|---|---|---|
| 5.1 | Logs sin passwords, tokens, o connection strings | ☐ | Revisar toda la capa de logging |
| 5.2 | Logs rotan diariamente con retención configurable | ☐ | Serilog o equivalente |
| 5.3 | Evento `VIEW_REPORT` auditado por `AuditService` | ☐ | CLAUDE.md regla 5 |
| 5.4 | Errores de extracción registrados con timestamp y causa | ☐ | Sin stack trace que exponga paths internos |
| 5.5 | Logs accesibles para debugging sin exponer datos cliente | ☐ | Enmascarar datos sensibles si aparecen |

---

## 6. Backup y Continuidad

| # | Control | Estado | Notas |
|---|---|---|---|
| 6.1 | Backup de Supabase configurado (punto-en-tiempo) | ☐ | Mínimo diario |
| 6.2 | Procedimiento de restauración documentado y probado | ☐ | Runbook de disaster recovery |
| 6.3 | Extractor puede re-ejecutarse sin duplicar datos (idempotente) | ☐ | DELETE + INSERT por company_id + período |
| 6.4 | MART puede regenerarse completo desde RAW en caso de corrupción | ☐ | `refresh_finance_mart(company_id)` |

---

## 7. Monitoreo de errores

| # | Control | Estado | Notas |
|---|---|---|---|
| 7.1 | Alerta si extractor no corre en más de 25 horas | ☐ | Cron monitor o health endpoint |
| 7.2 | Alerta si `OACT count = 0` después de extracción | ☐ | Posible error de conexión SAP |
| 7.3 | Alerta si `OJDT count = 0` en día hábil | ☐ | SAP sin asientos — inusual |
| 7.4 | Alerta si MART refresh falla | ☐ | Log en tabla de errores |
| 7.5 | Endpoint `GET /api/client/bi/finance/refresh-status` respondiendo | ☐ | Uptime monitor |

---

## Estado general del hardening

```
[ ] Hardening completo — listo para primer cliente real
[ ] Hardening parcial — anotar ítems pendientes con fecha
[ ] Hardening no iniciado — NO onboardear cliente real
```

**Fecha de revisión:** _______________  
**Revisado por:** _______________
