using Newtonsoft.Json;

namespace EInvoiceQuickBooks.Models1
{
    public class InvoiceRequest
    {
        [JsonProperty("e-Invoice Version")]
        public string? eInvoiceVersion { get; set; }

        [JsonProperty("e-Invoice Type Code")]
        public string? eInvoiceTypeCode { get; set; }

        [JsonProperty("e-Invoice Code or Number")]
        public string? eInvoiceCodeOrNumber { get; set; }

        [JsonProperty("Source Invoıce Number")]
        public string? SourceInvoiceNumber { get; set; }

        [JsonProperty("e-Invoice Date")]
        public string? eInvoiceDate { get; set; }

        [JsonProperty("e-Invoice Time")]
        public string? eInvoiceTime { get; set; }

        [JsonProperty("Invoice Currency Code")]
        public string? InvoiceCurrencyCode { get; set; }

        [JsonProperty("Currency Exchange Rate")]
        public string? CurrencyExchangeRate { get; set; }

        [JsonProperty("Payment Mode")]
        public string? PaymentMode { get; set; }

        [JsonProperty("Payment Terms")]
        public string? PaymentTerms { get; set; }

        [JsonProperty("Payment due date")]
        public string? PaymentDueDate { get; set; }

        [JsonProperty("Bill Reference Number")]
        public string? BillReferenceNumber { get; set; }

        [JsonProperty("Seller Bank Account Number")]
        public string? SellerBankAccountNumber { get; set; }

        [JsonProperty("Seller Name")]
        public string? SellerName { get; set; }

        [JsonProperty("Seller TIN")]
        public string? SellerTIN { get; set; }

        [JsonProperty("Seller Category")]
        public string? SellerCategory { get; set; }

        [JsonProperty("Seller Business Registration Number")]
        public string? SellerBusinessRegistrationNumber { get; set; }

        [JsonProperty("Seller SST Registration Number")]
        public string? SellerSSTRegistrationNumber { get; set; }

        [JsonProperty("Seller e-mail")]
        public string? SellerEmail { get; set; }

        [JsonProperty("Seller Malaysia Standard Industrial Classification Code")]
        public string? SellerMalaysiaStandardIndustrialClassificationCode { get; set; }

        [JsonProperty("Seller Contact Number")]
        public string? SellerContactNumber { get; set; }

        [JsonProperty("Seller Address Line 0")]
        public string? SellerAddressLine0 { get; set; }

        [JsonProperty("Seller Address Line 1")]
        public string? SellerAddressLine1 { get; set; }

        [JsonProperty("Seller Address Line 2")]
        public string? SellerAddressLine2 { get; set; }

        [JsonProperty("Seller Postal Zone")]
        public string? SellerPostalZone { get; set; }

        [JsonProperty("Seller City Name")]
        public string? SellerCityName { get; set; }

        [JsonProperty("Seller State")]
        public string? SellerState { get; set; }

        [JsonProperty("Seller Country")]
        public string? SellerCountry { get; set; }

        [JsonProperty("Seller Business Activity Description")]
        public string? SellerBusinessActivityDescription { get; set; }

        [JsonProperty("Seller MSIC")]
        public string? SellerMSIC { get; set; }

        [JsonProperty("Buyer Name")]
        public string? BuyerName { get; set; }

        [JsonProperty("Buyer TIN")]
        public string? BuyerTIN { get; set; }

        [JsonProperty("Buyer Category")]
        public string? BuyerCategory { get; set; }

        [JsonProperty("Buyer Busıness Registration Number")]
        public string? BuyerBusinessRegistrationNumber { get; set; }

        [JsonProperty("Buyer Identification Number or Passport Number")]
        public string? BuyerIdentificationNumberOrPassportNumber { get; set; }

        [JsonProperty("Buyer SST Registration Number")]
        public string? BuyerSSTRegistrationNumber { get; set; }

        [JsonProperty("Buyer e-mail")]
        public string? BuyerEmail { get; set; }

        [JsonProperty("Buyer Contact Number")]
        public string? BuyerContactNumber { get; set; }

        [JsonProperty("Buyer Address Line 0")]
        public string? BuyerAddressLine0 { get; set; }

        [JsonProperty("Buyer Address Line 1")]
        public string? BuyerAddressLine1 { get; set; }

        [JsonProperty("Buyer Address Line 2")]
        public string? BuyerAddressLine2 { get; set; }

        [JsonProperty("Buyer Postal Zone")]
        public string? BuyerPostalZone { get; set; }

        [JsonProperty("Buyer City Name")]
        public string? BuyerCityName { get; set; }

        [JsonProperty("Buyer State")]
        public string? BuyerState { get; set; }

        [JsonProperty("Buyer Country")]
        public string? BuyerCountry { get; set; }

        [JsonProperty("Sum of Invoice line net amount")]
        public string? SumOfInvoiceLineNetAmount { get; set; }

        [JsonProperty("Sum of allowances on document level")]
        public string? SumOfAllowancesOnDocumentLevel { get; set; }

        [JsonProperty("Total Fee or Charge Amount")]
        public string? TotalFeeOrChargeAmount { get; set; }

        [JsonProperty("Total Excluding Tax")]
        public string? TotalExcludingTax { get; set; }

        [JsonProperty("Total Including Tax")]
        public string? TotalIncludingTax { get; set; }

        [JsonProperty("Rounding amount")]
        public string? RoundingAmount { get; set; }

        [JsonProperty("Paid amount")]
        public string? PaidAmount { get; set; }

        [JsonProperty("Total Payable Amount")]
        public string? TotalPayableAmount { get; set; }

        [JsonProperty("Reference Number of Customs Form No 1 ID")]
        public string? ReferenceNumberOfCustomsFormNo1ID { get; set; }

        [JsonProperty("Reference Number of Customs Form No 1 Document Type")]
        public string? ReferenceNumberOfCustomsFormNo1DocumentType { get; set; }

        [JsonProperty("Incoterms")]
        public string? Incoterms { get; set; }

        [JsonProperty("Free Trade Agreement DocumentType")]
        public string? FreeTradeAgreementDocumentType { get; set; }

        [JsonProperty("Free Trade Agreement ID")]
        public string? FreeTradeAgreementID { get; set; }

        [JsonProperty("Free Trade Agreement Document Description")]
        public string? FreeTradeAgreementDocumentDescription { get; set; }

        [JsonProperty("Authorisation Number for Certified Exporter")]
        public string? AuthorisationNumberForCertifiedExporter { get; set; }

        [JsonProperty("Authorisation Number for Certified Exporter Agency Name")]
        public string? AuthorisationNumberForCertifiedExporterAgencyName { get; set; }

        [JsonProperty("Reference Number of Customs Form No 2 ID")]
        public string? ReferenceNumberOfCustomsFormNo2ID { get; set; }

        [JsonProperty("Reference Number of Customs Form No 2 Document Type")]
        public string? ReferenceNumberOfCustomsFormNo2DocumentType { get; set; }

        [JsonProperty("Details of other charges ID")]
        public string? DetailsOfOtherChargesID { get; set; }

        [JsonProperty("Details of other charges ChargeIndicator")]
        public string? DetailsOfOtherChargesChargeIndicator { get; set; }

        [JsonProperty("Details of other charges Amount")]
        public string? DetailsOfOtherChargesAmount { get; set; }

        [JsonProperty("Details of other charges AllowanceChargeReason")]
        public string? DetailsOfOtherChargesAllowanceChargeReason { get; set; }

        [JsonProperty("Total Net Amount")]
        public string? TotalNetAmount { get; set; }

        [JsonProperty("InvoiceLine")]
        //public List<LineItem>? LineItem { get; set; }
        public List<LineItem>? InvoiceLine { get; set; }

        [JsonProperty("isPDF")]
        public bool? isPDF { get; set; }

        [JsonProperty("Output Format")]
        public string? OutputFormat { get; set; }

        [JsonProperty("Source Name")]
        public string? SourceName { get; set; }

        [JsonProperty("SourceFileName")]
        public string? SourceFileName { get; set; }

        [JsonProperty("Tax Office Scheduler Template Name")]
        public string? TaxOfficeSchedulerTemplateName { get; set; }
        
        [JsonProperty("Template Name")]
        public string? TemplateName { get; set; }

        [JsonProperty("quickBookDetails")]
        public string? quickBookDetails { get; set; }

        [JsonProperty("DocTaxTotal")]
        public DocTaxTotal? DocTaxTotal { get; set; }

        [JsonProperty("AllowanceCharges")]
        public List<AllowanceCharge>? AllowanceCharges { get; set; }
    }

    public class LineItem
    {
        [JsonProperty("LineId")]
        public string? LineId { get; set; }

        [JsonProperty("Classification Class")]
        public string? ClassificationClass { get; set; }

        [JsonProperty("Classification Code")]
        public string? ClassificationCode { get; set; }

        [JsonProperty("Product ID")]
        public string? ProductID { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Product Tariff Code")]
        public string? ProductTariffCode { get; set; }

        [JsonProperty("Product Tariff Class")]
        public string? ProductTariffClass { get; set; }

        [JsonProperty("Country")]
        public string? Country { get; set; }

        [JsonProperty("Unit Price")]
        public string? UnitPrice { get; set; }

        [JsonProperty("Quantity")]
        public string? Quantity { get; set; }

        [JsonProperty("Measurement")]
        public string? Measurement { get; set; }

        [JsonProperty("Subtotal")]
        public string? Subtotal { get; set; }

        [JsonProperty("SST Tax Category")]
        public string? SSTTaxCategory { get; set; }

        [JsonProperty("Tax Type")]
        public string? TaxType { get; set; }

        [JsonProperty("Tax Rate")]
        public string? TaxRate { get; set; }

        [JsonProperty("Tax Amount")]
        public string? TaxAmount { get; set; }

        [JsonProperty("Details of Tax Exemption")]
        public string? DetailsOfTaxExemption { get; set; }
        [JsonProperty("Amount Exempted from Tax")]
        public string? AmountExemptedFromTax { get; set; }
        [JsonProperty("Total Excluding Tax")]
        public string? TotalExcludingTax { get; set; }
        [JsonProperty("Invoice line net amount")]
        public string? InvoiceLineNetAmount { get; set; }
        [JsonProperty("Nett Amount")]
        public string? NettAmount { get; set; }

        [JsonProperty("TaxCategory schemeID")]
        public string? TaxCategorySchemeID { get; set; }
        [JsonProperty("TaxCategory schemeAgencyID")]
        public string? TaxCategorySchemeAgencyID { get; set; }
        [JsonProperty("TaxCategory schemeAgency code")]
        public string? TaxCategorySchemeAgencyCode { get; set; }

        //[JsonProperty("Gross Amount")]
        //public string? GrossAmount { get; set; }

        //[JsonProperty("Tax Code")]
        //public string? TaxCode { get; set; }

        //[JsonProperty("Account Code")]
        //public string? AccountCode { get; set; }

        //[JsonProperty("Amount Paid")]
        //public string? AmountPaid { get; set; }

        //[JsonProperty("Amount Due")]
        //public string? AmountDue { get; set; }
    }

    public class DocTaxTotal
    {
        [JsonProperty("TAX category tax amount in accounting currency")]
        public string? TaxCategoryTaxAmountInAccountingCurrency { get; set; }

        [JsonProperty("Total Taxable Amount Per Tax Type")]
        public string? TotalTaxableAmountPerTaxType { get; set; }

        [JsonProperty("TaxCategory Id")]
        public string? TaxCategoryId { get; set; }
        [JsonProperty("TaxCategory TaxScheme Id")]
        public string? TaxCategoryTaxSchemeId { get; set; }

        [JsonProperty("TaxCategory schemeAgencyID")]
        public string? TaxCategorySchemeAgencyID { get; set; }
        [JsonProperty("TaxCategory schemeAgency code")]
        public string? TaxCategorySchemeAgencyCode { get; set; }

        [JsonProperty("TAX category rate")]
        public string? TaxCategoryRate { get; set; }
        [JsonProperty("Details of Tax Exemption")]
        public string? DetailsOfTaxExemption { get; set; }
    }

    public class AllowanceCharge
    {
    }
}
