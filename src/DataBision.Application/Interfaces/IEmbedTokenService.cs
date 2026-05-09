using DataBision.Application.DTOs.Reports;

namespace DataBision.Application.Interfaces;

public interface IEmbedTokenService
{
    Task<EmbedTokenResponseDto> GenerateAsync(int reportId, string companySlug);
}
