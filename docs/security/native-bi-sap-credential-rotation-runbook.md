# Native BI — Runbook de Rotación de Credenciales SAP

**DataBision · Junio 2026**  
**Versión:** 1.0 — Gate 3 pre-deployment Modalidad A  
**Aplica a:** Modalidad A — Extractor instalado con perfil de conexión SAP

---

## Principio de diseño

El extractor usa arquitectura **one-shot**: cada ejecución inicia, lee las credenciales desde el SecretRef (variable de entorno o Azure Key Vault), conecta a SAP, extrae datos y termina. Esto significa:

- **No hay estado en memoria entre ejecuciones**
- **Cambiar la variable de entorno es suficiente** — el próximo ciclo programado usará la nueva credencial sin necesidad de reiniciar ningún servicio
- **No se requiere redeploy de código**

Si en algún momento se usa modo `--service` (Windows Service permanente), ese proceso sí requiere reinicio tras la rotación de credenciales. Este runbook cubre ambos casos.

---

## ¿Cuándo rotar la password SAP?

| Evento | Acción requerida |
|---|---|
| Política de seguridad (periodicidad acordada) | Rotación planificada |
| Sospecha de compromiso de credencial | Rotación de emergencia |
| Cambio de usuario SAP DATABISION_RO | Rotación completa (usuario + contraseña) |
| Expiración forzada por SAP B1 | Rotación planificada |
| Offboarding de personal con acceso al servidor del extractor | Rotación de emergencia |

---

## Rotación planificada (modo one-shot scheduler)

### Paso 1 — Cambiar la contraseña en SAP B1

> **NUNCA imprimir ni compartir la nueva contraseña por chat, email ni log.**

1. Acceder a SAP B1 → Administration → Security → Users → `DATABISION_RO`
2. Cambiar la contraseña siguiendo la política de complejidad del cliente
3. Anotar la nueva contraseña de forma segura (gestor de contraseñas)
4. Confirmar el cambio (SAP puede requerir cerrar y abrir sesión)

### Paso 2 — Actualizar la variable de entorno en el servidor del extractor

**Windows (como Administrador):**

```powershell
# Actualizar la variable de sistema (sin mostrar el valor en pantalla)
# Solicitar el valor al operador por input seguro
$newPassword = Read-Host -AsSecureString "Nueva password SAP DATABISION_RO"
$plainPw = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($newPassword))

[System.Environment]::SetEnvironmentVariable(
    "SAP_PASSWORD_KSDEPOR",   # nombre real de la variable según appsettings
    $plainPw,
    "Machine"   # nivel System — no User
)

# Limpiar la variable temporal
Clear-Variable plainPw
```

> **Nota:** El nombre de la variable de entorno corresponde al `SecretRef` configurado en el perfil. Si el SecretRef es `env:SAP_PASSWORD_KSDEPOR`, la variable de entorno es `SAP_PASSWORD_KSDEPOR`.

**Linux:**

```bash
# Editar el archivo de env vars restringido
sudo nano /etc/databision.env

# Cambiar la línea correspondiente:
# SAP_PASSWORD_KSDEPOR=<nueva-password>
# Guardar y cerrar el editor

# Verificar permisos (no deben ser world-readable)
ls -la /etc/databision.env
# Esperado: -rw-r----- root databision
```

### Paso 3 — Verificar que el SecretRef resuelve correctamente (dry-run)

```bash
# En el servidor del extractor:
dotnet /opt/databision/extractor/DataBision.Extractor.dll \
    --profile ksdepor-prd \
    --dry-run
```

Verificar en la salida:
- `SAP credentials loaded from profile` — OK
- No aparece mensaje de error de SecretRef
- El `CompanyDB` mostrado es el correcto

### Paso 4 — Verificar login a SAP (test de conexión desde panel Admin)

```http
POST /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}/test
```

Resultado esperado:
```json
{
  "data": {
    "success": true,
    "capabilities": {
      "loginOk": true,
      "chartOfAccountsOk": true
    }
  }
}
```

Si el test falla con "Login failed (HTTP 401)":
- La nueva contraseña en SAP no coincide con la variable de entorno
- Verificar que el cambio en SAP fue guardado correctamente
- Verificar que el nombre de la variable de entorno coincide con el SecretRef del perfil

### Paso 5 — Ejecutar extracción manual de verificación

```bash
dotnet /opt/databision/extractor/DataBision.Extractor.dll \
    --profile ksdepor-prd \
    --object OJDT \
    --run-once --send
```

Verificar exit code = 0 y log de extracción sin errores de autenticación.

### Paso 6 — Documentar la rotación

Registrar en el log de cambios de seguridad:

```
Fecha: YYYY-MM-DD
Operador: [nombre]
Tipo: Rotación planificada de contraseña SAP DATABISION_RO
Cliente: [nombre del cliente]
Empresa SAP: [CompanyDB]
Perfil afectado: ksdepor-prd (id=N)
Test de conexión post-rotación: SUCCESS
Extracción OJDT post-rotación: EXIT_CODE=0
```

---

## Rotación de emergencia

Si se sospecha compromiso de credencial, ejecutar los pasos en este orden sin demora:

1. **Inmediato:** Deshabilitar usuario `DATABISION_RO` en SAP B1 (Administration → Security → Users → Block User)
2. Detener tareas programadas temporalmente:
   - Windows: `Disable-ScheduledTask -TaskName "DataBision-*"`
   - Linux: `sudo systemctl stop databision-ojdt.timer databision-mart.timer`
3. Cambiar contraseña del usuario `DATABISION_RO` en SAP B1
4. Actualizar la variable de entorno (Paso 2 del runbook normal)
5. Rehabilitar el usuario en SAP B1
6. Ejecutar test de conexión (Paso 4)
7. Reactivar tareas programadas:
   - Windows: `Enable-ScheduledTask -TaskName "DataBision-*"`
   - Linux: `sudo systemctl start databision-ojdt.timer databision-mart.timer`
8. Documentar el incidente

---

## Si el extractor corre como Windows Service (futuro)

Si se usa `--service` (proceso permanente), la rotación requiere un paso adicional después de actualizar la variable de entorno:

```powershell
# Reiniciar el servicio para que lea la nueva variable
Restart-Service -Name "DataBisionExtractor"

# Verificar estado
Get-Service -Name "DataBisionExtractor"
```

> Este es el único escenario donde se requiere reinicio. Con el modo one-shot (recomendado para MVP), el reinicio no es necesario.

---

## Diferencia entre modos de ejecución

| Aspecto | One-Shot (Scheduler) | Windows Service (--service) |
|---|---|---|
| **Cuándo lee credenciales** | En cada inicio del proceso | Solo al arrancar el servicio |
| **Post-rotación** | No requiere ninguna acción adicional | Requiere reinicio del servicio |
| **Riesgo si falla rotación** | Solo la siguiente ejecución falla (exit code 3) | Proceso permanece con credencial vencida |
| **Recomendación MVP** | ✅ Usar este modo | ⚠️ Evitar para primer cliente |

---

## Criterio GO Gate 3

| Criterio | Estado |
|---|---|
| Existe procedimiento documentado de rotación | ✅ Este documento |
| Rotación no requiere redeploy de código | ✅ Solo actualización de variable de entorno |
| Rotación no imprime password en ningún log | ✅ Scripts usan `Read-Host -AsSecureString` en Windows, edición directa en Linux |
| Dry-run post-rotación confirma resolución | ✅ Paso 3 del runbook |
| Test de conexión post-rotación confirmado | ✅ Paso 4 del runbook |

**Estado Gate 3: GO ✅** (sujeto a que el operador siga el runbook)
