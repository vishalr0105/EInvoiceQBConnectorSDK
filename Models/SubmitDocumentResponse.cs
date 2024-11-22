namespace EInvoiceQuickBooks.Models
{
    public class SubmitDocumentResponse
    {
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public bool? IsSuccess { get; set; }
        public object? Data { get; set; }
    }

    public class AcceptedDocument
    {
        public string? Uuid { get; set; }
        public string? InvoiceCodeNumber { get; set; }
    }

    public class Data
    {
        public string? InvoiceId { get; set; }
        public string? Uuid { get; set; }
        public string? InvoiceCodeNumber { get; set; }
    }

    public class LoginData
    {
        public string? Token { get; set; }
        public string? TokenLifeTime { get; set; }
    }
}
