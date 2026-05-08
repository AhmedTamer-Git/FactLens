using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class RssProxyController : ControllerBase
{
    private readonly HttpClient _http;

    public RssProxyController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
    }

    [HttpGet]
    public async Task<IActionResult> Get(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Missing url");

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/xml");
        }
        catch
        {
            return BadRequest("Failed to fetch RSS");
        }
    }
}