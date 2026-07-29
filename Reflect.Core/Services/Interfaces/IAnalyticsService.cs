using Reflect.Models.Analytics;

namespace Reflect.Services.Interfaces;

// Works out the numbers shown on the dashboard.
public interface IAnalyticsService
{
    // Whole journal rather than the selected range - a streak doesn't mean much
    // if it gets cut off at the edge of the range.
    Task<StreakSummary> GetStreaksAsync();

    // Everything else in one call, so the dashboard isn't running a separate
    // query for each panel.
    Task<AnalyticsSummary> GetSummaryAsync(DateTime from, DateTime to);
}
