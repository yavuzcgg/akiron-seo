namespace AkironSeo.Application.Common.Interfaces;

public record AiSeoRecommendationDto(
    string OptimizedTitle,
    string OptimizedMetaDescription,
    List<string> TargetKeywords,
    List<string> ActionableTips
);

public interface IAiOptimizationService
{
    Task<AiSeoRecommendationDto> GenerateSeoRecommendationsAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
