# DataBision Extractor — Windows Service Installation Guide

## Requirements

| Requirement | Detail |
|---|---|
| OS | Windows Server 2019+ or Windows 10/11 |
| Runtime | .NET 8.0 Runtime (x64) — not SDK |
| SAP B1 | Service Layer accessible from the server |
| DataBision API | DataBision Ingest API URL and API key |
| Network | Firewall rules open to SAP SL port (typically 50000) and DataBision API |
| Permissions | Local administrator to install Windows Service |

---

## 1. Publish the Extractor

From the project root on a dev machine:

```powershell
dotnet publish src\DataBision.Extractor\DataBision.Extractor.csproj `
  -c Release -r win-x64 --self-contained false `
  -o C:\DataBision\Extractor\publish
```

---

## 2. Prepare the Installation Folder

```powershell
New-Item -ItemType Directory -Force -Path "C:\DataBision\Extractor"
New-Item -ItemType Directory -Force -Path "C:\DataBision\Extractor\logs"

# Copy published output
Copy-Item "publish\*" "C:\DataBision\Extractor\" -Recurse

# Copy and rename config template
Copy-Item "src\DataBision.Extractor\appsettings.Production.template.json" `
          "C:\DataBision\Extractor\appsettings.Production.json"
```

---

## 3. Configure appsettings.Production.json

Edit `C:\DataBision\Extractor\appsettings.Production.json` and fill in the placeholders:

```json
{
  "SapServiceLayer": {
    "BaseUrl":   "https://192.168.1.100:50000/b1s/v1",
    "CompanyDB": "SBODEMOAR",
    "UserName":  "manager",
    "Password":  "** set via environment variable **"
  },
  "DataBisionApi": {
    "BaseUrl": "https://api.databision.app",
    "ApiKey":  "** set via environment variable **"
  },
  "Extractor": {
    "TenantId":        "your-tenant-id",
    "CompanyId":       "company-dev-001",
    "Objects":         [ "OCRD", "OITM", "OINV", "ORIN" ],
    "IntervalMinutes": 30,
    "SendEnabled":     true
  }
}
```

> **Never store passwords in versioned files.**

---

## 4. Set Secrets via Environment Variables (Recommended)

Instead of writing secrets in appsettings.Production.json, set Windows environment variables using the `__` separator convention:

```powershell
[System.Environment]::SetEnvironmentVariable(
  "SapServiceLayer__Password", "your_sap_password",
  [System.EnvironmentVariableTarget]::Machine)

[System.Environment]::SetEnvironmentVariable(
  "DataBisionApi__ApiKey", "your_api_key",
  [System.EnvironmentVariableTarget]::Machine)
```

The extractor reads machine-level environment variables at startup.

---

## 5. Set ASPNETCORE_ENVIRONMENT (or DOTNET_ENVIRONMENT)

```powershell
[System.Environment]::SetEnvironmentVariable(
  "DOTNET_ENVIRONMENT", "Production",
  [System.EnvironmentVariableTarget]::Machine)
```

This causes the extractor to load `appsettings.Production.json` automatically.

---

## 6. Install as Windows Service

```cmd
sc.exe create DataBisionExtractor ^
  binPath= "C:\DataBision\Extractor\DataBision.Extractor.exe --service" ^
  DisplayName= "DataBision SAP Extractor" ^
  start= delayed-auto
```

**PowerShell alternative:**

```powershell
New-Service -Name "DataBisionExtractor" `
            -BinaryPathName '"C:\DataBision\Extractor\DataBision.Extractor.exe" --service' `
            -DisplayName "DataBision SAP Extractor" `
            -StartupType Automatic
```

---

## 7. Start / Stop / Status

```powershell
Start-Service DataBisionExtractor
Stop-Service DataBisionExtractor
Get-Service DataBisionExtractor
Restart-Service DataBisionExtractor
```

Or via sc.exe:

```cmd
sc.exe start DataBisionExtractor
sc.exe stop  DataBisionExtractor
sc.exe query DataBisionExtractor
```

---

## 8. Logs

Log files are written to:

```
C:\DataBision\Extractor\logs\databision-extractor-YYYYMMDD.log
```

Rolling daily, retained for 30 days.

**Never logged:** passwords, API keys, cookies, session IDs.

---

## 9. Test Without Installing as Service

```powershell
# Test one cycle with SendEnabled=true from config
C:\DataBision\Extractor\DataBision.Extractor.exe --schedule --max-cycles 1

# Or test via dotnet run in dev:
dotnet run --project src\DataBision.Extractor -- --schedule --interval-minutes 1 --max-cycles 1 --send
```

---

## 10. Uninstall

```cmd
sc.exe stop DataBisionExtractor
sc.exe delete DataBisionExtractor
```

---

## 11. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Cannot connect to Service Layer | Firewall / wrong URL | Check `SapServiceLayer.BaseUrl` and port 50000 |
| SSL certificate error | Self-signed cert | Set `IgnoreSslCertificateErrors: true` |
| HTTP 401 on login | Wrong credentials | Verify `UserName` + `Password` env vars |
| Cannot connect to DataBision API | Wrong BaseUrl or ApiKey | Verify `DataBisionApi.BaseUrl` and ApiKey env var |
| Service fails to start | Missing .NET 8 runtime | Install .NET 8 Runtime from microsoft.com |
| Logs folder not writable | Permissions | Grant write permissions on `C:\DataBision\Extractor\logs\` to the service account |
| Service account permissions | Not enough rights | Run service as Local System or a dedicated account with network access |
| No data in Supabase | `SendEnabled` is false | Set `Extractor.SendEnabled: true` in appsettings.Production.json |
