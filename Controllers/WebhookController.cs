using EInvoiceQuickBooks.Services;
using Intuit.Ipp.Data;
using Intuit.Ipp.WebhooksService;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;

namespace EInvoiceQuickBooks.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IQueueService _webhookProcessingService;

        public WebhookController(IQueueService webhookProcessingService)
        {
            _webhookProcessingService = webhookProcessingService;
        }

        //working
        [HttpPost("ReceiveWebhook")]
        public IActionResult ReceiveWebhook([FromBody] JsonElement payload)
        {
            try
            {
                Log.Information($"\n\n-----ReceiveWebhook called-----\n\n{payload}");

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

                var operation = doc.RootElement.GetProperty("eventNotifications")[0]
                                      .GetProperty("dataChangeEvent")
                                      .GetProperty("entities")[0]
                                      .GetProperty("operation")
                                      .GetString();

                var entity = doc.RootElement.GetProperty("eventNotifications")[0]
                                      .GetProperty("dataChangeEvent")
                                      .GetProperty("entities")[0]
                                      .GetProperty("name")
                                      .GetString();

                var operationList = new List<string>() { "Emailed", "Delete", "Create", "Update", "Void" };
                var entityList = new List<string>() { "Invoice" };

                if (operationList.Contains(operation) && entityList.Contains(entity))
                {
                    _webhookProcessingService.Enqueue(payloadString);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ReceiveWebhook - {ex}");

                return BadRequest(ex.Message);
            }
        }
    }
}
