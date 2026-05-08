using Factlens.Core.DTOs;

namespace Factlens.Services.Interfaces
{
    public interface IAiOrchestrator
    {
        Task<AiResponse> CheckNewsAndSaveAsync(string userId, string text);
        Task<AiResponse> CheckNewsImageAndSaveAsync(string userId, Stream imageStream, string fileName);
    }
}