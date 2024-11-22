using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Services;
using Microsoft.AspNetCore.Mvc;

namespace EInvoiceQuickBooks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly InvoiceService _invoiceService;
        private readonly Dictionary<string, string> _urlStore = new();

        public InvoiceController(InvoiceService invoiceSevice)
        {
            _invoiceService = invoiceSevice;
        }

        #region short url
        //[NonAction]
        [HttpPost("shorten")]
        public async Task<IActionResult> ShortenUrl([FromBody] UrlRequest urlRequest)
        {
            if (string.IsNullOrEmpty(urlRequest.Url))
                return BadRequest("Invalid URL.");

            var shortUrl = await ShortenUrlAsync(urlRequest.Url);
            return Ok(new { ShortUrl = shortUrl });
        }
        //[NonAction]
        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
        {
            var originalUrl = await GetOriginalUrlAsync(shortCode);

            if (originalUrl == null)
                return NotFound("URL not found.");

            return Redirect(originalUrl);
        }
        private Task<string> ShortenUrlAsync(string originalUrl)
        {
            var shortCode = GenerateShortCode();
            _urlStore[shortCode] = originalUrl;
            return Task.FromResult($"https://yourdomain.com/api/urlshortener/{shortCode}");
        }

        private Task<string> GetOriginalUrlAsync(string shortCode)
        {
            _urlStore.TryGetValue(shortCode, out var originalUrl);
            return Task.FromResult(originalUrl);
        }

        private string GenerateShortCode()
        {
            // Create a random alphanumeric short code (e.g., 6 characters long)
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(characters, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion
    }
}
