# Native BI — Seguridad de Credenciales SAP

**Sprint:** 21B  
**Fecha:** 2026-06-20

---

## Estado actual (DEV/TST)

Las credenciales SAP (URL, CompanyDB, UserName, Password) están en `appsettings.Development.json`:

- **Archivo en disco:** Protegido por permisos del OS si el servidor está bien configurado
- **No cifrado:** Password en texto plano
- **No en repositorio:** `.gitignore` excluye `appsettings.Development.json`
- **Riesgo:** Si el disco se compromete, o el archivo se envía por email/chat accidentalmente

Este modelo es aceptable para DEV y TST (ambientes de prueba). No es aceptable para producción con datos reales del cliente.

---

## Estrategia MVP — SecretRef + config de entorno

Para la primera versión productiva, las credenciales se inyectan como variables de entorno o via secrets del OS, y el extractor las lee sin almacenarlas en archivos:

### Windows — DPAPI (Data Protection API)

```powershell
# Cifrar password con DPAPI (solo se puede descifrar en el mismo usuario del OS)
$plaintext = "SapPassword123"
$encrypted = [System.Convert]::ToBase64String(
    [System.Security.Cryptography.ProtectedData]::Protect(
        [System.Text.Encoding]::UTF8.GetBytes($plaintext),
        $null,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser
    )
)

# Guardar en appsettings.Production.json como:
# { "SapServiceLayer": { "PasswordDpapi": "[BASE64]" } }
```

### Linux — ENV vars gestionadas por systemd

```ini
# /etc/systemd/system/databision-finance-[SLUG].service
[Service]
EnvironmentFile=/opt/databision/secrets/[SLUG].env
# /opt/databision/secrets/[SLUG].env (chmod 600, owner=databision)
# SAP_PASSWORD=...
```

### Cloud — Azure Key Vault (recomendado para producción escalada)

```json
// appsettings.Production.json
{
  "SapServiceLayer": {
    "BaseUrl": "https://...",
    "CompanyDB": "...",
    "UserName": "...",
    "PasswordSecretRef": "azure-kv://databision-prod/sap-password-[slug]"
  }
}
```

El extractor llama a Azure Key Vault SDK para resolver el secret en runtime. Requiere Managed Identity o Service Principal con permiso `secrets/get`.

---

## Estrategia futura — DB cifrada con AES-256-GCM

Cuando `NativeBiConnectionProfile` esté implementado (Sprint 22):

```
Password del cliente (plaintext)
    ↓
AES-256-GCM encrypt
    key = DEK (Data Encryption Key) único por empresa
    DEK cifrado con KEK (Key Encryption Key) en Azure Key Vault
    ↓
sap_password_encrypted (base64) → almacenado en AppDB
```

Solo el proceso que tiene acceso al KEK puede descifrar. El DBA sin acceso al vault no puede ver el password.

---

## Reglas de seguridad (OBLIGATORIAS para producción)

1. **Nunca imprimir credenciales en logs.** El extractor enmascara URLs (`MaskUrl`). El password nunca se loguea.
2. **TLS siempre en prod.** `IgnoreSslCertificateErrors = false` en producción. Solo true para clientes con certificados autofirmados (documentar en cada caso).
3. **Usuario SAP read-only.** Nunca usar el usuario manager o administrador de SAP como credencial del extractor.
4. **Rotación de credenciales.** Si el password SAP cambia, actualizar inmediatamente el perfil de conexión. El extractor fallará en el próximo ciclo y generará alerta.
5. **Principio de mínimo privilegio.** El usuario del OS que ejecuta el extractor (Windows Service / systemd) no tiene más permisos que acceso a la carpeta del extractor y red al host SAP.
6. **No hardcodear company-id en scripts.** Usar `--company [ANALYTICS_COMPANY_ID]` como parámetro, no como string literal en el script.

---

## Checklist de seguridad antes de producción

- [ ] `IgnoreSslCertificateErrors = false` (o documentado como excepción)
- [ ] Password SAP en ENV var o gestor de secrets — no en appsettings.json en texto plano
- [ ] `appsettings.Production.json` tiene `chmod 600` (Linux) o permisos NTFS restringidos (Windows)
- [ ] Usuario SAP es exclusivo para DataBision (no compartido con otros procesos)
- [ ] Logs del extractor revisados — confirmar que no aparece el password ni la connection string completa
- [ ] `git status` confirma que ningún archivo de secrets está en staging
- [ ] Archivo `.gitignore` incluye `appsettings.*.json` (excepto `appsettings.json` base y template)
