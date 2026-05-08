namespace Factlens.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<object> SummaryAsync();
        Task<object> RequestsAsync(int page, int pageSize, int? status, string? search);
        Task<object> VerdictsAsync();
        Task<object> FeedbackAsync(int page, int pageSize, bool? reportedOnly);
    }
}
