using DataBision.Application.Interfaces;

namespace DataBision.Application.Services;

public sealed class DefaultSecretRefResolver : ISecretRefResolver
{
    public string Resolve(string secretRef) => SecretRefResolver.Resolve(secretRef);
    public string ToHint(string secretRef)  => SecretRefResolver.ToHint(secretRef);
}
