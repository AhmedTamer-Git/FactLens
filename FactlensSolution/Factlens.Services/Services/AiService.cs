using Factlens.Core.DTOs;
using System.Net.Http.Json;

namespace Factlens.Services.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;

        public AiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ✅ Text / URL verification  →  /verify-text
        public async Task<AiResponse> CheckNewsAsync(string newsText)
        {
            var response = await _httpClient.PostAsJsonAsync("verify-text", new { text = newsText });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI API error: {(int)response.StatusCode}");

            return await response.Content.ReadFromJsonAsync<AiResponse>()
                   ?? throw new Exception("Empty AI response");
        }

        // ✅ Image verification  →  /verify-image
        public async Task<AiResponse> CheckNewsImageAsync(Stream imageStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);

            // تحديد الـ content type بناءً على امتداد الملف
            var contentType = fileName.ToLower() switch
            {
                var f when f.EndsWith(".png") => "image/png",
                var f when f.EndsWith(".jpg") => "image/jpeg",
                var f when f.EndsWith(".jpeg") => "image/jpeg",
                var f when f.EndsWith(".webp") => "image/webp",
                _ => "image/jpeg"
            };

            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("verify-image", content);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI Image API error: {(int)response.StatusCode}");

            return await response.Content.ReadFromJsonAsync<AiResponse>()
                   ?? throw new Exception("Empty AI image response");
        }
    }
}