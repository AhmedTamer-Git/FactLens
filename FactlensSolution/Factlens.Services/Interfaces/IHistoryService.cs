namespace Factlens.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<object> GetAsync(string userId, string? search, string? verdict, DateTime? from, DateTime? to, int page, int pageSize);
        Task DeleteAsync(string userId, int id);
        Task ClearAsync(string userId);
    }
}
