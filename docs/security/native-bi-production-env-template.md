# Native BI Finance — Template Variables de Entorno (Producción)

**Sprint 27 · DataBision · Junio 2026**  
**IMPORTANTE: Este archivo es una plantilla sin valores. Nunca commitear valores reales.**

---

## Uso

Copiar este archivo como `.env` en el servidor de producción y completar con los valores reales.  
El archivo `.env` debe estar en `.gitignore` y nunca ser versionado.

---

## DataBision API — Variables de entorno

```env
# ─── Entorno ────────────────────────────────────────────────────────────────
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# ─── JWT ────────────────────────────────────────────────────────────────────
# RSA private key en formato PEM (todo en una línea o multilinea con \n)
Jwt__PrivateKey=

# RSA public key en formato PEM
Jwt__PublicKey=

# ─── Base de datos principal ─────────────────────────────────────────────────
# Azure SQL o PostgreSQL connection string
ConnectionStrings__DefaultConnection=

# Base de datos de staging (Supabase PostgreSQL)
ConnectionStrings__StagingConnection=

# ─── Ingest API Keys (una por cliente) ───────────────────────────────────────
# Formato: Ingest__ApiKeys__[NOMBRE_CLIENTE_UPPER]
# Reemplazar NOMBRE_CLIENTE con el slug del cliente en mayúsculas
Ingest__ApiKeys__KSDEPOR=
# Ingest__ApiKeys__CLIENTE2=
# Ingest__ApiKeys__CLIENTE3=

# ─── Credenciales SAP por cliente ────────────────────────────────────────────
# Referenciadas como SecretRef: env:SAP_PASSWORD_[CLIENTE]
# Formato: SAP_PASSWORD_[NOMBRE_CLIENTE_UPPER]
SAP_PASSWORD_KSDEPOR=
# SAP_PASSWORD_CLIENTE2=
# SAP_PASSWORD_CLIENTE3=

# ─── DataBision API (self-reference para el extractor) ───────────────────────
DataBisionApi__BaseUrl=
DataBisionApi__ApiKey=

# ─── Azure (opcional — para Key Vault futuro) ────────────────────────────────
# Azure__TenantId=
# Azure__ClientId=
# Azure__ClientSecret=
# Azure__KeyVaultUrl=

# ─── Power BI (si aplica) ────────────────────────────────────────────────────
# PowerBI__TenantId=
# PowerBI__ClientId=
# PowerBI__ClientSecret=
# PowerBI__WorkspaceId=

# ─── App ─────────────────────────────────────────────────────────────────────
App__BaseDomain=databision.app

# ─── Azure Blob Storage (logos de tenant, si aplica) ─────────────────────────
# Azure__BlobStorageConnectionString=
```

---

## Notas de seguridad

| Variable | Nivel de sensibilidad | Storage recomendado |
|---|---|---|
| `Jwt__PrivateKey` | Crítico | Azure Key Vault o HSM |
| `Jwt__PublicKey` | Público | Puede estar en config |
| `ConnectionStrings__DefaultConnection` | Crítico | Azure Key Vault |
| `ConnectionStrings__StagingConnection` | Crítico | Azure Key Vault |
| `Ingest__ApiKeys__*` | Alto | Variables de entorno en servidor |
| `SAP_PASSWORD_*` | Crítico | Variables de entorno en servidor |
| `DataBisionApi__ApiKey` | Alto | Variables de entorno en servidor |
| `PowerBI__ClientSecret` | Crítico | Azure Key Vault |

---

## Validación post-deploy

Verificar que ninguna variable crítica esté vacía antes de iniciar el servicio:

```bash
# Verificar variables críticas (sin imprimir valores)
[ -z "$Jwt__PrivateKey" ] && echo "ERROR: Jwt__PrivateKey no configurado"
[ -z "$ConnectionStrings__StagingConnection" ] && echo "ERROR: StagingConnection no configurado"
[ -z "$DataBisionApi__ApiKey" ] && echo "ERROR: DataBisionApi__ApiKey no configurado"
```

---

## Rotación de API Keys

Para rotar la API key de un cliente sin downtime:

1. Generar nueva key: `openssl rand -base64 32`
2. Agregar nueva key como variable adicional temporalmente:
   ```
   Ingest__ApiKeys__CLIENTE_NEW=[nueva_key]
   ```
3. Actualizar el extractor del cliente para usar la nueva key
4. Verificar que el extractor funciona con la nueva key
5. Eliminar la key antigua del archivo `.env`
6. Reiniciar el API server

---

## Rotación de password SAP

Si el password SAP de un cliente cambia:

1. Actualizar `SAP_PASSWORD_[CLIENTE]` en el archivo `.env` del servidor
2. Reiniciar el servicio del extractor (o recargar variables de entorno)
3. Ejecutar un test de conexión manual:
   ```bash
   dotnet run --project src/DataBision.Extractor -- --profile [cliente] --dry-run
   ```
4. Confirmar que la extracción siguiente corre sin error de autenticación
