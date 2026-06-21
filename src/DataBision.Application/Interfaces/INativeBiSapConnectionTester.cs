using DataBision.Application.DTOs.Admin;
using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface INativeBiSapConnectionTester
{
    Task<TestNativeBiConnectionProfileResult> TestAsync(
        NativeBiConnectionProfile profile, CancellationToken ct = default);
}
