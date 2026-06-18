using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface IAccountClassificationService
{
    Task<IReadOnlyList<AccountClassificationRuleDto>> GetRulesAsync(string analyticsCompanyId, CancellationToken ct = default);
    Task<AccountClassificationRuleDto> CreateRuleAsync(string analyticsCompanyId, UpsertAccountClassificationRuleDto dto, CancellationToken ct = default);
    Task<AccountClassificationRuleDto?> UpdateRuleAsync(string analyticsCompanyId, int ruleId, UpsertAccountClassificationRuleDto dto, CancellationToken ct = default);
    Task<bool> DeleteRuleAsync(string analyticsCompanyId, int ruleId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountClassificationTemplateSuggestionDto>> GetTemplateSuggestionsAsync(string analyticsCompanyId, CancellationToken ct = default);
}
