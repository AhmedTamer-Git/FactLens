using Factlens.Core.DTOs;

namespace Factlens.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task AddAsync(string userId, FeedbackRequest request);
    }
}
