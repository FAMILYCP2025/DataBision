namespace DataBision.Application.Interfaces;

public interface ISecretRefResolver
{
    string Resolve(string secretRef);
    string ToHint(string secretRef);
}
