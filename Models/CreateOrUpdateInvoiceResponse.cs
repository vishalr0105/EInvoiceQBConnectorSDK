namespace EInvoiceQuickBooks.Models
{
    public class CreateOrUpdateInvoiceResponse
    {
        public string? Status { get; set; }
        public CreateResponseToMapDB? Data { get; set; }
        public object? Error { get; set; }
    }

    public class CreateResponseToMapDB
    {
        public string? Id { get; set; }
        public string? SyncToken { get; set; }
        public string? MetaData { get; set; }   
        public string? CustomField { get; set; }
        public string? DocNumber { get; set; }
    }
}
