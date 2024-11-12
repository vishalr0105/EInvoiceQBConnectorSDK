using EInvoiceQuickBooks.Services;
using Intuit.Ipp.WebhooksService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Text.Json;

namespace EInvoiceQuickBooks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        //string logFilePath = @"C:\QBLogs\WebHookServiceLogs.txt";
        private readonly ILogger<WebhookController> _logger;
        private readonly IQueueService _webhookProcessingService;

        public WebhookController(ILogger<WebhookController> logger, IQueueService webhookProcessingService)
        {
            _webhookProcessingService = webhookProcessingService;
            _logger = logger;
            //using (StreamWriter writer = new StreamWriter(logFilePath, true))
            //{
            //    writer.WriteLine($"\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} - In Webhook constructor");
            //}
        }

        //working
        [HttpPost("ReceiveWebhook")]
        public IActionResult ReceiveWebhook([FromBody] JsonElement payload)
        {
            try
            {
                Log.Information("ReceiveWebhook called");

                var payloadString = payload.GetRawText();
                var intuitSignature = Request.Headers["intuit-signature"].ToString();

                if (string.IsNullOrEmpty(intuitSignature))
                {
                    Log.Information("Missing Intuit signature.");
                    return BadRequest("Missing Intuit signature.");
                }

                var obj = new WebhooksService();
                var test = obj.VerifyPayload(intuitSignature, payloadString);

                if (test == false)
                {
                    Log.Information("Invalid signature.");
                    return Unauthorized("Invalid signature.");
                }

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
                var jsonEx = JsonConvert.SerializeObject(ex);
                Log.Information($"{jsonEx}");

                return BadRequest(ex.Message);
            }
        }
    }
}
