namespace EInvoiceQuickBooks.Models
{
    public class ProcessRequest
    {
        public string? emailAddress { get; set; }
        public string? base64Pdf { get; set; }
        public string? invoiceNo { get; set; }
    }
}
