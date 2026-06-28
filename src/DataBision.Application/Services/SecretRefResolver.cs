namespace DataBision.Application.Services;

/// <summary>
/// Resolves a SecretRef string to the secret value at runtime.
/// Supported schemes for Sprint 22: env:VARIABLE_NAME
/// Planned (not yet implemented): azure-kv://, user-secrets://, dpapi://
/// </summary>
public static class SecretRefResolver
{
    public static string Resolve(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
            throw new InvalidOperationException("SecretRef is required but was empty.");

        if (secretRef.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var varName = secretRef["env:".Length..].Trim();
            if (string.IsNullOrWhiteSpace(varName))
                throw new InvalidOperationException("SecretRef 'env:' scheme requires a variable name (e.g. env:MY_VAR).");

            var value = Environment.GetEnvironmentVariable(varName);
            if (value is null)
                throw new InvalidOperationException(
                    $"Environment variable '{varName}' referenced by SecretRef is not set.");

            return value;
        }

        if (secretRef.StartsWith("local-dev-only:", StringComparison.OrdinalIgnoreCase))
        {
            var value = secretRef["local-dev-only:".Length..];
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("SecretRef 'local-dev-only:' scheme requires a value after the colon.");

            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "SecretRef scheme 'local-dev-only:' is not allowed in Production environments. " +
                    "Use 'env:VARIABLE_NAME' instead.");

            return value;
        }

        // Schemes planned but not yet implemented
        if (secretRef.StartsWith("azure-kv://", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("SecretRef scheme 'azure-kv://' is not yet implemented. Use 'env:' for now.");

        if (secretRef.StartsWith("user-secrets:", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("SecretRef scheme 'user-secrets:' is not yet implemented. Use 'env:' for now.");

        if (secretRef.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("SecretRef scheme 'dpapi:' is not yet implemented. Use 'env:' for now.");

        throw new InvalidOperationException(
            $"Unknown SecretRef scheme in '{MaskSecretRef(secretRef)}'. Supported: env:VARIABLE_NAME, local-dev-only:value");
    }

    /// <summary>Returns the scheme prefix only — safe to log or return in API responses.</summary>
    public static string ToHint(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef)) return "(empty)";
        var colonIdx = secretRef.IndexOf(':');
        return colonIdx >= 0
            ? secretRef[..(colonIdx + 1)] + "***"
            : "***";
    }

    private static string MaskSecretRef(string secretRef)
    {
        var colonIdx = secretRef.IndexOf(':');
        return colonIdx >= 0
            ? secretRef[..(colonIdx + 1)] + "***"
            : "***";
    }
}
