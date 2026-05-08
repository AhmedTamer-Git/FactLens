using Factlens.Core.DTOs;

namespace Factlens.Services.Interfaces
{
    public interface IShareService
    {
        Task<SharedResultDto> GetSharedAsync(string shareId);


        Task<byte[]> ExportImageAsync(string shareId);
    }
}
