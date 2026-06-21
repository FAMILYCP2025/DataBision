# Native BI — SecretRef Resolver

**Sprint:** 22D  
**Fecha:** 2026-06-21

---

## Propósito

`SecretRefResolver` resuelve referencias a secretos en runtime sin persistir el valor en ningún lugar. Se usa para obtener el password de SAP Service Layer desde `NativeBiConnectionProfile.SecretRef`.

---

## Clase

`DataBision.Application.Services.SecretRefResolver` (static)

---

## Esquemas soportados (Sprint 22)

### `env:VARIABLE_NAME`

Lee la variable de entorno del proceso.

```
SecretRef: env:DATABISION_SAP_PASSWORD_KSDEPOR
```

Resultado: valor de `Environment.GetEnvironmentVariable("DATABISION_SAP_PASSWORD_KSDEPOR")`

Si la variable no existe → `InvalidOperationException` con el nombre de la variable.

### `local-dev-only:valor`

Para desarrollo local solamente. El valor va directamente después del `:`.

```
SecretRef: local-dev-only:mi-password-dev
```

**No usar en producción.**

---

## Esquemas planificados (no implementados todavía)

| Esquema | Estado | Plan |
|---|---|---|
| `azure-kv://vault/secret` | No implementado | Sprint 23A |
| `user-secrets:section:key` | No implementado | Sprint 23A |
| `dpapi:base64-ciphertext` | No implementado | Sprint 23B |

Intentar usar esquemas no implementados lanza `NotSupportedException`.

---

## Método `ToHint`

Retorna representación segura para logs y respuestas de API:

| Input | Output |
|---|---|
| `env:DATABISION_SAP_PASSWORD_KSDEPOR` | `env:***` |
| `azure-kv://my-vault/my-secret` | `azure-kv:***` |
| `local-dev-only:password` | `local-dev-only:***` |
| *(vacío)* | `(empty)` |

---

## Tests

`DataBision.Application.Tests.Services.SecretRefResolverTests` cubre:

- Resolución de `env:` existente
- Error en `env:` inexistente
- Error en `env:` sin nombre de variable
- `local-dev-only:` con valor
- Error en `local-dev-only:` vacío
- `NotSupportedException` para `azure-kv://`
- Error para esquema desconocido
- Error para string vacío/whitespace
- `ToHint()` 5 casos

---

## Configuración del servidor (producción)

1. Crear variable de entorno por empresa en el servidor del extractor:
   ```
   DATABISION_SAP_PASSWORD_KSDEPOR=<password SAP>
   ```
2. Configurar SecretRef en el perfil:
   ```
   SecretRef: env:DATABISION_SAP_PASSWORD_KSDEPOR
   ```
3. Verificar que la variable está disponible en el proceso del API (para test-connection)

**Convención de nombres:**
```
DATABISION_SAP_PASSWORD_{COMPANY_SLUG_UPPERCASE}
```

Ejemplo: cliente `ksdepor` → `DATABISION_SAP_PASSWORD_KSDEPOR`

---

## Reglas de seguridad

- `SecretRefResolver.Resolve()` nunca se llama en controllers ni DTOs
- El valor resuelto nunca se asigna a una variable con nombre `password` en logs
- `NativeBiSapConnectionTester` llama a Resolve, usa el valor en el payload HTTP, y no lo retiene ni registra
- La respuesta de test-connection nunca incluye el password
