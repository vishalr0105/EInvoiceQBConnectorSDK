using EInvoiceQuickBooks.Services;
using Intuit.Ipp.WebhooksService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EInvoiceQuickBooks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        string logFilePath = "D:\\Logs\\WebHookServiceLogs.txt";
        private readonly ILogger<WebhookController> _logger;
        private readonly IQueueService _webhookProcessingService;

        public WebhookController(ILogger<WebhookController> logger, IQueueService webhookProcessingService)
        {
            _webhookProcessingService = webhookProcessingService;
            _logger = logger;
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} - In Webhook constructor");
            }
        }

        //working
        [HttpPost("ReceiveWebhook")]
        public IActionResult ReceiveWebhook([FromBody] JsonElement payload)
        {
            try
            {
                var payloadString = payload.GetRawText();
                var intuitSignature = Request.Headers["intuit-signature"].ToString();

                if (string.IsNullOrEmpty(intuitSignature))
                {
                    LogInfo($"Missing Intuit signature.");
                    return BadRequest("Missing Intuit signature.");
                }

                var obj = new WebhooksService();
                var test = obj.VerifyPayload(intuitSignature, payloadString);

                if (test == false)
                {
                    LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Invalid signature");
                    return Unauthorized("Invalid signature.");
                }
                LogInfo($"Webhook received and verified.");

                using JsonDocument doc = JsonDocument.Parse(payloadString);
                string operation = doc.RootElement
                                      .GetProperty("eventNotifications")[0]
                                      .GetProperty("dataChangeEvent")
                                      .GetProperty("entities")[0]
                                      .GetProperty("operation")
                                      .GetString();

                if (operation == "Emailed" || operation == "Delete" || operation == "Create" || operation == "Update")
                {
                    _webhookProcessingService.Enqueue(payloadString);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private void LogInfo(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - INFO - {message}");
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
