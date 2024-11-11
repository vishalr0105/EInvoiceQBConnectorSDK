namespace EInvoiceQuickBooks.Models
{
    public class QuickBooksSettings
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        //public string? AccessToken { get; set; }
        //public string? RealmId { get; set; }
        public string? BaseUrl { get; set; }
        public string? RefreshToken { get; set; }
    }
}
