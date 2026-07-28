using Reflect.Models.Analytics;

namespace Reflect.Services.Interfaces;

/// <summary>
/// Aggregates entries into the figures shown on the dashboard.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Journalling consistency across the whole journal. Not range-filtered -
    /// see <see cref="StreakSummary"/> for why.
    /// </summary>
    Task<StreakSummary> GetStreaksAsync();

    /// <summary>
    /// Every dashboard figure for one inclusive date range, gathered in a single
    /// call so the page issues a fixed number of queries.
    /// </summary>
    Task<AnalyticsSummary> GetSummaryAsync(DateTime from, DateTime to);
}
