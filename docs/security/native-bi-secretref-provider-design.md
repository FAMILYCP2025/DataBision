# Native BI — SecretRef Provider Design

**Sprint:** 23D  
**Fecha:** 2026-06-21

---

## Motivación

`SecretRefResolver` (Sprint 22D) era una clase estática sin interfaz. Para Sprint 23 se extrajo `ISecretRefResolver` para:

1. Permitir inyección de dependencia en servicios que necesitan resolver secretos
2. Facilitar testing con mocks sin necesitar ENV vars reales
3. Preparar para implementar `azure-kv://` sin modificar consumidores existentes

---

## Arquitectura

```
ISecretRefResolver
  └─ DefaultSecretRefResolver  ← implementa vía delegación a static SecretRefResolver
  └─ (futuro) AzureKeyVaultSecretRefResolver

SecretRefResolver (static) ← backward compat para tests directos
```

### Interfaz

```csharp
public interface ISecretRefResolver
{
    string Resolve(string secretRef);
    string ToHint(string secretRef);
}
```

### Implementación por defecto

```csharp
public sealed class DefaultSecretRefResolver : ISecretRefResolver
{
    public string Resolve(string secretRef) => SecretRefResolver.Resolve(secretRef);
    public string ToHint(string secretRef)  => SecretRefResolver.ToHint(secretRef);
}
```

### Registro en DI (API)

```csharp
builder.Services.AddSingleton<ISecretRefResolver, DefaultSecretRefResolver>();
```

---

## Consumidores

| Clase | Antes | Después |
|---|---|---|
| `NativeBiSapConnectionTester` | `SecretRefResolver.Resolve()` (static) | `ISecretRefResolver.Resolve()` (injected) |
| `NativeBiConnectionProfileService` | `SecretRefResolver.ToHint()` (static en ToDto) | `ISecretRefResolver.ToHint()` (injected) |
| `NativeBiConnectionProfileService.ResolveForExtractorAsync` | N/A (Sprint 23) | `ISecretRefResolver.Resolve()` (injected) |

---

## Tests existentes (sin cambio)

`DataBision.Application.Tests.Services.SecretRefResolverTests` usa la clase estática directamente — no necesita cambios porque `SecretRefResolver` static sigue existiendo. Los 14 tests existentes pasan sin modificación.

---

## Esquemas soportados (sin cambio)

| Esquema | Estado | Notas |
|---|---|---|
| `env:VARIABLE_NAME` | ✅ Implementado | Principal para producción |
| `local-dev-only:value` | ✅ Implementado | Solo DEV, nunca producción |
| `azure-kv://vault/secret` | ❌ NotSupportedException | Backlog — implementar `AzureKeyVaultSecretRefResolver` |
| `user-secrets:key` | ❌ NotSupportedException | Backlog |
| `dpapi:base64` | ❌ NotSupportedException | Backlog |

---

## Diseño para azure-kv:// (próximo sprint)

Cuando se implemente:

```csharp
public sealed class AzureKeyVaultSecretRefResolver : ISecretRefResolver
{
    private readonly SecretClient _client;
    
    public AzureKeyVaultSecretRefResolver(SecretClient client) => _client = client;
    
    public string Resolve(string secretRef)
    {
        if (secretRef.StartsWith("azure-kv://"))
        {
            // parse vault URL + secret name
            // return _client.GetSecret(name).Value.Value
        }
        // delegate others to DefaultSecretRefResolver or throw
    }
    
    public string ToHint(string secretRef) => SecretRefResolver.ToHint(secretRef);
}
```

Registro:
```csharp
if (builder.Configuration["AzureKeyVault:VaultUri"] is { } vaultUri)
{
    builder.Services.AddSingleton<ISecretRefResolver>(sp =>
        new AzureKeyVaultSecretRefResolver(new SecretClient(new Uri(vaultUri), new DefaultAzureCredential())));
}
else
{
    builder.Services.AddSingleton<ISecretRefResolver, DefaultSecretRefResolver>();
}
```

No se agregan paquetes Azure hasta que se active esta ruta.
