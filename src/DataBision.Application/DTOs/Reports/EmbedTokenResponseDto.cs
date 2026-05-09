namespace DataBision.Application.DTOs.Reports;

public record EmbedTokenResponseDto(
    string EmbedUrl,
    string AccessToken,
    string TokenId,
    DateTime Expiry);
