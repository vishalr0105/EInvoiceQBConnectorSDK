using System.Text.Json.Serialization;

namespace EInvoiceQuickBooks.Models
{
    public class FullInvoiceModel
    {

        [JsonPropertyName("invoice")]
        public DBInvoice Invoice { get; set; }

        [JsonPropertyName("lineItems")]
        public List<LineItemDB> LineItems { get; set; }

        [JsonPropertyName("docTaxSubTotal")]
        public DocTaxSubTotal DocTaxSubTotal { get; set; }
    }

    public class DBInvoice
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("taxOfficeSchedulerId")]
        public int? TaxOfficeSchedulerId { get; set; }

        [JsonPropertyName("participantId")]
        public int? ParticipantId { get; set; }

        [JsonPropertyName("participantName")]
        public string? ParticipantName { get; set; }

        [JsonPropertyName("sourceFileName")]
        public string? SourceFileName { get; set; }

        [JsonPropertyName("eInvoiceNumber")]
        public string? EInvoiceNumber { get; set; }

        [JsonPropertyName("sourcename")]
        public string? SourceName { get; set; }

        [JsonPropertyName("outputFormat")]
        public string? OutputFormat { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("cacPostalSellerAddress")]
        public string? CacPostalSellerAddress { get; set; }

        [JsonPropertyName("cbcIdentificationCode")]
        public string? CbcIdentificationCode { get; set; }

        [JsonPropertyName("cbcSellerCompanyID")]
        public string? CbcSellerCompanyID { get; set; }

        [JsonPropertyName("cbcInvoiceTypeCode")]
        public string? CbcInvoiceTypeCode { get; set; }

        [JsonPropertyName("cbcIssueDate")]
        public string? CbcIssueDate { get; set; }

        [JsonPropertyName("cbcLineExtensionAmount")]
        public string? CbcLineExtensionAmount { get; set; }

        [JsonPropertyName("cbcSellerName")]
        public string? CbcSellerName { get; set; }

        [JsonPropertyName("cbcBuyerName")]
        public string? CbcBuyerName { get; set; }

        [JsonPropertyName("cbcNameDeliverToPartyName")]
        public string? CbcNameDeliverToPartyName { get; set; }

        [JsonPropertyName("cbcBuyerCompanyID")]
        public string? CbcBuyerCompanyID { get; set; }

        [JsonPropertyName("validationLink")]
        public string? ValidationLink { get; set; }

        [JsonPropertyName("cacPostalBuyerAddress")]
        public string? CacPostalBuyerAddress { get; set; }

        [JsonPropertyName("cbcSellerVATID")]
        public string? CbcSellerVATID { get; set; }

        [JsonPropertyName("cbcBuyerVATID")]
        public string? CbcBuyerVATID { get; set; }

        [JsonPropertyName("cbcCompanyLegalForm")]
        public string? CbcCompanyLegalForm { get; set; }

        [JsonPropertyName("cbcDescription")]
        public string? CbcDescription { get; set; }

        [JsonPropertyName("cacPrice")]
        public string? CacPrice { get; set; }

        [JsonPropertyName("cacTaxTotal")]
        public string? CacTaxTotal { get; set; }

        [JsonPropertyName("eInvoiceType")]
        public string? EInvoiceType { get; set; }

        [JsonPropertyName("eTemplateId")]
        public int? ETemplateId { get; set; }

        [JsonPropertyName("templateId")]
        public int? TemplateId { get; set; }

        [JsonPropertyName("taxid")]
        public string? TaxId { get; set; }

        [JsonPropertyName("regnNo")]
        public string? RegnNo { get; set; }

        [JsonPropertyName("sstRegnNo")]
        public string? SstRegnNo { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("eInvoiceDateTime")]
        public DateTime? EInvoiceDateTime { get; set; }

        [JsonPropertyName("addressLine1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("addressLine2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("cacInvoicePeriod")]
        public string? CacInvoicePeriod { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("invoiceValidator")]
        public string? InvoiceValidator { get; set; }

        [JsonPropertyName("taxOfficeSubmitter")]
        public string? TaxOfficeSubmitter { get; set; }

        [JsonPropertyName("cbcPrecedingInvoicenumber")]
        public string? CbcPrecedingInvoiceNumber { get; set; }

        [JsonPropertyName("cbcIDPaymentAccountIdentifier")]
        public string? CbcIDPaymentAccountIdentifier { get; set; }

        [JsonPropertyName("cacPaymentTerms")]
        public string? CacPaymentTerms { get; set; }

        [JsonPropertyName("cbcIDVATcategoryCode")]
        public string? CbcIDVATCategoryCode { get; set; }

        [JsonPropertyName("cacAddress")]
        public string? CacAddress { get; set; }

        [JsonPropertyName("cbcIDInvoiceNumber")]
        public string? CbcIDInvoiceNumber { get; set; }

        [JsonPropertyName("cbcSellerElectronicMail")]
        public string? CbcSellerElectronicMail { get; set; }

        [JsonPropertyName("cbcBuyerElectronicMail")]
        public string? CbcBuyerElectronicMail { get; set; }

        [JsonPropertyName("cbcBuyerReference")]
        public string? CbcBuyerReference { get; set; }

        [JsonPropertyName("cbcDocumentCurrencyCode")]
        public string? CbcDocumentCurrencyCode { get; set; }

        [JsonPropertyName("cbcTaxCurrencyCode")]
        public string? CbcTaxCurrencyCode { get; set; }

        [JsonPropertyName("cbcPayableAmount")]
        public string? CbcPayableAmount { get; set; }

        [JsonPropertyName("cbcPaymentID")]
        public string? CbcPaymentID { get; set; }

        [JsonPropertyName("cbcPercent")]
        public string? CbcPercent { get; set; }

        [JsonPropertyName("cbcSellerRegnName")]
        public string? CbcSellerRegnName { get; set; }

        [JsonPropertyName("cbcBuyerRegnName")]
        public string? CbcBuyerRegnName { get; set; }

        [JsonPropertyName("cbcTaxableAmount")]
        public string? CbcTaxableAmount { get; set; }

        [JsonPropertyName("cbcTaxExclusiveAmount")]
        public string? CbcTaxExclusiveAmount { get; set; }

        [JsonPropertyName("cbcTaxExemptionReason")]
        public string? CbcTaxExemptionReason { get; set; }

        [JsonPropertyName("cbcTaxInclusiveAmount")]
        public string? CbcTaxInclusiveAmount { get; set; }

        [JsonPropertyName("cbcTaxPointDate")]
        public string? CbcTaxPointDate { get; set; }

        [JsonPropertyName("cbcSellerTelephone")]
        public string? CbcSellerTelephone { get; set; }

        [JsonPropertyName("cbcBuyerTelephone")]
        public string? CbcBuyerTelephone { get; set; }

        [JsonPropertyName("cbcBusinessActivityDesc")]
        public string? CbcBusinessActivityDesc { get; set; }

        [JsonPropertyName("totalAllowanceAmount")]
        public string? TotalAllowanceAmount { get; set; }

        [JsonPropertyName("totalTaxAmount")]
        public string? TotalTaxAmount { get; set; }

        [JsonPropertyName("totalAmountDue")]
        public string? TotalAmountDue { get; set; }

        [JsonPropertyName("prePaidAmount")]
        public string? PrePaidAmount { get; set; }

        [JsonPropertyName("cbcPaidDate")]
        public string? CbcPaidDate { get; set; }

        [JsonPropertyName("cbcPaidTime")]
        public string? CbcPaidTime { get; set; }

        [JsonPropertyName("cbcPaidId")]
        public string? CbcPaidId { get; set; }

        [JsonPropertyName("payableRoundingAmount")]
        public string? PayableRoundingAmount { get; set; }

        [JsonPropertyName("totalChangeAmount")]
        public string? TotalChangeAmount { get; set; }

        [JsonPropertyName("totalLineAmount")]
        public string? TotalLineAmount { get; set; }

        [JsonPropertyName("cbcMSICCode")]
        public string? CbcMSICCode { get; set; }

        [JsonPropertyName("cbcIRBMValidationDate")]
        public DateTime? CbcIRBMValidationDate { get; set; }

        [JsonPropertyName("namePaymentMeansText")]
        public string? NamePaymentMeansText { get; set; }

        [JsonPropertyName("irbmUniqueNo")]
        public string? IrbmUniqueNo { get; set; }

        [JsonPropertyName("schemeID")]
        public string? SchemeID { get; set; }

        [JsonPropertyName("unitCode")]
        public string? UnitCode { get; set; }

        [JsonPropertyName("cbcPaymentCurrencyCode")]
        public string? CbcPaymentCurrencyCode { get; set; }

        [JsonPropertyName("paymentDueDate")]
        public DateTime? PaymentDueDate { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime? UpdatedDate { get; set; }

        [JsonPropertyName("cbcNote")]
        public string? CbcNote { get; set; }

        [JsonPropertyName("pdfBlob")]
        public byte[]? PdfBlob { get; set; }

        [JsonPropertyName("pdfXml")]
        public byte[]? PdfXml { get; set; }

        [JsonPropertyName("xmlWithQRBlob")]
        public byte[]? XmlWithQRBlob { get; set; }

        [JsonPropertyName("pdfWithQRBlob")]
        public byte[]? PdfWithQRBlob { get; set; }

        [JsonPropertyName("jsonInvoiceBlob")]
        public byte[]? JsonInvoiceBlob { get; set; }

        [JsonPropertyName("jsonWithQRBlob")]
        public byte[]? JsonWithQRBlob { get; set; }

        [JsonPropertyName("workflowStatus")]
        public string? WorkflowStatus { get; set; }

        [JsonPropertyName("invoiceVersion")]
        public string? InvoiceVersion { get; set; }

        [JsonPropertyName("cbcSellerSSTRegistrationNumber")]
        public string? CbcSellerSSTRegistrationNumber { get; set; }

        [JsonPropertyName("cbcSellerTourismTaxRegistrationNumber")]
        public string? CbcSellerTourismTaxRegistrationNumber { get; set; }

        [JsonPropertyName("cbcSStreetName")]
        public string? CbcSStreetName { get; set; }

        [JsonPropertyName("cbcSAdditionalStreetName1")]
        public string? CbcSAdditionalStreetName1 { get; set; }

        [JsonPropertyName("cbcSAdditionalStreetName2")]
        public string? CbcSAdditionalStreetName2 { get; set; }

        [JsonPropertyName("cbcSPostalZone")]
        public string? CbcSPostalZone { get; set; }

        [JsonPropertyName("cbcSCityName")]
        public string? CbcSCityName { get; set; }

        [JsonPropertyName("cbcSCountrySubentity")]
        public string? CbcSCountrySubentity { get; set; }

        [JsonPropertyName("cbcSCountryIdentificationCode")]
        public string? CbcSCountryIdentificationCode { get; set; }

        [JsonPropertyName("cbcBStreetName")]
        public string? CbcBStreetName { get; set; }

        [JsonPropertyName("cbcBAdditionalStreetName1")]
        public string? CbcBAdditionalStreetName1 { get; set; }

        [JsonPropertyName("cbcBAdditionalStreetName2")]
        public string? CbcBAdditionalStreetName2 { get; set; }

        [JsonPropertyName("cbcBPostalZone")]
        public string? CbcBPostalZone { get; set; }

        [JsonPropertyName("cbcBCityName")]
        public string? CbcBCityName { get; set; }

        [JsonPropertyName("cbcBCountrySubentity")]
        public string? CbcBCountrySubentity { get; set; }

        [JsonPropertyName("cbcBCountryIdentificationCode")]
        public string? CbcBCountryIdentificationCode { get; set; }

        [JsonPropertyName("cbcBSSTRegistrationNumber")]
        public string? CbcBSSTRegistrationNumber { get; set; }

        [JsonPropertyName("cbcDStreetName")]
        public string? CbcDStreetName { get; set; }

        [JsonPropertyName("cbcDAdditionalStreetName1")]
        public string? CbcDAdditionalStreetName1 { get; set; }

        [JsonPropertyName("cbcDAdditionalStreetName2")]
        public string? CbcDAdditionalStreetName2 { get; set; }

        [JsonPropertyName("cbcDPostalZone")]
        public string? CbcDPostalZone { get; set; }

        [JsonPropertyName("cbcDCityName")]
        public string? CbcDCityName { get; set; }

        [JsonPropertyName("cbcDCountrySubentity")]
        public string? CbcDCountrySubentity { get; set; }

        [JsonPropertyName("cbcDCountryIdentificationCode")]
        public string? CbcDCountryIdentificationCode { get; set; }

        [JsonPropertyName("cbcShipRecipientName")]
        public string? CbcShipRecipientName { get; set; }

        [JsonPropertyName("cbcShipRecipientVATID")]
        public string? CbcShipRecipientVATID { get; set; }

        [JsonPropertyName("cbcShipRecipientCompanyID")]
        public string? CbcShipRecipientCompanyID { get; set; }

        [JsonPropertyName("cbcShipRecipientStreetName")]
        public string? CbcShipRecipientStreetName { get; set; }

        [JsonPropertyName("cbcShipRecipientAdditionalStreetName1")]
        public string? CbcShipRecipientAdditionalStreetName1 { get; set; }

        [JsonPropertyName("cbcShipRecipientAdditionalStreetName2")]
        public string? CbcShipRecipientAdditionalStreetName2 { get; set; }

        [JsonPropertyName("cbcShipRecipientPostalZone")]
        public string? CbcShipRecipientPostalZone { get; set; }

        [JsonPropertyName("cbcShipRecipientCityName")]
        public string? CbcShipRecipientCityName { get; set; }

        [JsonPropertyName("cbcShipRecipientCountrySubentity")]
        public string? CbcShipRecipientCountrySubentity { get; set; }

        [JsonPropertyName("cbcShipRecipientCountryIdentificationCode")]
        public string? CbcShipRecipientCountryIdentificationCode { get; set; }

        [JsonPropertyName("cbcCalculationRate")]
        public string? CbcCalculationRate { get; set; }

        [JsonPropertyName("cbcStartDate")]
        public DateTime? CbcStartDate { get; set; }

        [JsonPropertyName("cbcEndDate")]
        public DateTime? CbcEndDate { get; set; }

        [JsonPropertyName("validityEndDateTime")]
        public DateTime? ValidityEndDateTime { get; set; }

        [JsonPropertyName("remainingHours")]
        public int? RemainingHours { get; set; }

        [JsonPropertyName("cbcSCategory")]
        public string? CbcSCategory { get; set; }

        [JsonPropertyName("cbcSSubCategory")]
        public string? CbcSSubCategory { get; set; }

        [JsonPropertyName("cbcSBRNNumber")]
        public string? CbcSBRNNumber { get; set; }

        [JsonPropertyName("cbcSNRIC")]
        public string? CbcSNRIC { get; set; }

        [JsonPropertyName("cbcBCategory")]
        public string? CbcBCategory { get; set; }

        [JsonPropertyName("cbcBSubCategory")]
        public string? CbcBSubCategory { get; set; }

        [JsonPropertyName("cbcBBRNNumber")]
        public string? CbcBBRNNumber { get; set; }

        [JsonPropertyName("cbcBNRIC")]
        public string? CbcBNRIC { get; set; }

        [JsonPropertyName("cbcShipRecipientCategory")]
        public string? CbcShipRecipientCategory { get; set; }

        [JsonPropertyName("cbcShipRecipientSubCategory")]
        public string? CbcShipRecipientSubCategory { get; set; }

        [JsonPropertyName("cbcShipRecipientBRNNumber")]
        public string? CbcShipRecipientBRNNumber { get; set; }

        [JsonPropertyName("cbcShipRecipientNRIC")]
        public string? CbcShipRecipientNRIC { get; set; }

        [JsonPropertyName("cbcItemClassificationCodeClass")]
        public string? CbcItemClassificationCodeClass { get; set; }

        [JsonPropertyName("cbcItemClassificationCodePTC")]
        public string? CbcItemClassificationCodePTC { get; set; }

        [JsonPropertyName("irbmUniqueIdentifierNumber")]
        public string? IrbmUniqueIdentifierNumber { get; set; }

        [JsonPropertyName("invoiceDocumentReferenceNumber")]
        public string? InvoiceDocumentReferenceNumber { get; set; }

        [JsonPropertyName("cbcCustomizationID")]
        public string? CbcCustomizationID { get; set; }

        [JsonPropertyName("cbcProfileID")]
        public string? CbcProfileID { get; set; }

        [JsonPropertyName("cbcDueDate")]
        public DateTime? CbcDueDate { get; set; }

        [JsonPropertyName("cbcAccountingCost")]
        public string? CbcAccountingCost { get; set; }

        [JsonPropertyName("cbcOrderReferenceId")]
        public string? CbcOrderReferenceId { get; set; }

        [JsonPropertyName("cbcSalesOrderID")]
        public string? CbcSalesOrderID { get; set; }

        [JsonPropertyName("cbcEndpointId")]
        public string? CbcEndpointId { get; set; }

        [JsonPropertyName("cbcEndpointIdschemeID")]
        public string? CbcEndpointIdschemeID { get; set; }

        [JsonPropertyName("cbcPartyTaxSchemeCompanyID")]
        public string? CbcPartyTaxSchemeCompanyID { get; set; }

        [JsonPropertyName("cbcPartyTaxSchemeID")]
        public string? CbcPartyTaxSchemeID { get; set; }

        [JsonPropertyName("cbcPartyLegalEntityCompanyID")]
        public string? CbcPartyLegalEntityCompanyID { get; set; }

        [JsonPropertyName("cbcPartyLegalEntityCompanyLegalForm")]
        public string? CbcPartyLegalEntityCompanyLegalForm { get; set; }

        [JsonPropertyName("cbcBuyerEndpointId")]
        public string? CbcBuyerEndpointId { get; set; }

        [JsonPropertyName("cbcBuyerEndpointIdschemeID")]
        public string? CbcBuyerEndpointIdschemeID { get; set; }

        [JsonPropertyName("cbcBuyerPartyTaxSchemeCompanyID")]
        public string? CbcBuyerPartyTaxSchemeCompanyID { get; set; }

        [JsonPropertyName("cbcBuyerPartyTaxSchemeID")]
        public string? CbcBuyerPartyTaxSchemeID { get; set; }

        [JsonPropertyName("cbcBuyerPartyLegalEntityCompanyID")]
        public string? CbcBuyerPartyLegalEntityCompanyID { get; set; }

        [JsonPropertyName("cbcBuyerPartyLegalEntityCompanyLegalForm")]
        public string? CbcBuyerPartyLegalEntityCompanyLegalForm { get; set; }

        [JsonPropertyName("cbcActualDeliveryDate")]
        public DateTime? CbcActualDeliveryDate { get; set; }

        [JsonPropertyName("cbcDeliveryLocationId")]
        public string? CbcDeliveryLocationId { get; set; }

        [JsonPropertyName("cbcDeliveryStreetName")]
        public string? CbcDeliveryStreetName { get; set; }

        [JsonPropertyName("cbcDeliveryAdditionalStreetName")]
        public string? CbcDeliveryAdditionalStreetName { get; set; }

        [JsonPropertyName("cbcDeliveryCityName")]
        public string? CbcDeliveryCityName { get; set; }

        [JsonPropertyName("cbcDeliveryPostalZone")]
        public string? CbcDeliveryPostalZone { get; set; }

        [JsonPropertyName("cbcDeliveryAddressLine")]
        public string? CbcDeliveryAddressLine { get; set; }

        [JsonPropertyName("cbcDeliveryCountryIdentificationCode")]
        public string? CbcDeliveryCountryIdentificationCode { get; set; }

        [JsonPropertyName("cacDeliveryPartyName")]
        public string? CacDeliveryPartyName { get; set; }

        [JsonPropertyName("etlJobName")]
        public string? EtlJobName { get; set; }

        [JsonPropertyName("cbcPricingCurrencyCode")]
        public string? CbcPricingCurrencyCode { get; set; }

        [JsonPropertyName("cbcCurrencyExchangeRate")]
        public string? CbcCurrencyExchangeRate { get; set; }

        [JsonPropertyName("cbcFrequencyofBilling")]
        public string? CbcFrequencyofBilling { get; set; }

        [JsonPropertyName("paymentMode")]
        public string? PaymentMode { get; set; }

        [JsonPropertyName("cbcSupplierBankAccountNumber")]
        public string? CbcSupplierBankAccountNumber { get; set; }

        [JsonPropertyName("cbcBillReferenceNumber")]
        public string? CbcBillReferenceNumber { get; set; }

        [JsonPropertyName("cbcTaxRate")]
        public string? CbcTaxRate { get; set; }

        [JsonPropertyName("cbcTaxCategory")]
        public string? CbcTaxCategory { get; set; }

        [JsonPropertyName("customsForm19ID")]
        public string? CustomsForm19ID { get; set; }

        [JsonPropertyName("customsForm19DocumentType")]
        public string? CustomsForm19DocumentType { get; set; }

        [JsonPropertyName("incoterms")]
        public string? Incoterms { get; set; }

        [JsonPropertyName("ftaDocumentType")]
        public string? FtaDocumentType { get; set; }

        [JsonPropertyName("ftaid")]
        public string? Ftaid { get; set; }

        [JsonPropertyName("ftaDocumentDesc")]
        public string? FtaDocumentDesc { get; set; }

        [JsonPropertyName("schemeAgencyName")]
        public string? SchemeAgencyName { get; set; }

        [JsonPropertyName("customsForm2ID")]
        public string? CustomsForm2ID { get; set; }

        [JsonPropertyName("customsForm2DocumentType")]
        public string? CustomsForm2DocumentType { get; set; }

        [JsonPropertyName("otherChargesID")]
        public string? OtherChargesID { get; set; }

        [JsonPropertyName("otherChargesChargeIndicator")]
        public string? OtherChargesChargeIndicator { get; set; }

        [JsonPropertyName("otherChargesAmount")]
        public string? OtherChargesAmount { get; set; }

        [JsonPropertyName("otherChargesAllowanceChargeReason")]
        public string? OtherChargesAllowanceChargeReason { get; set; }

        [JsonPropertyName("cbcBillingPeriodStartDate")]
        public DateTime? CbcBillingPeriodStartDate { get; set; }

        [JsonPropertyName("cbcBillingPeriodEndDate")]
        public DateTime? CbcBillingPeriodEndDate { get; set; }

        [JsonPropertyName("İnvoiceFactorycalcutionMode")]
        public int? InvoiceFactorycalcutionMode { get; set; }

        [JsonPropertyName("etlCalculationMode")]
        public int? EtlCalculationMode { get; set; }

        [JsonPropertyName("sourceCalculationMode")]
        public int? SourceCalculationMode { get; set; }

        [JsonPropertyName("notificationTemplateId")]
        public string? NotificationTemplateId { get; set; }

        [JsonPropertyName("smsTemplateId")]
        public string? SmsTemplateId { get; set; }

        [JsonPropertyName("notifications")]
        public string? Notifications { get; set; }

        [JsonPropertyName("inputFormat")]
        public string? InputFormat { get; set; }

        [JsonPropertyName("submittedXml")]
        public string? SubmittedXml { get; set; }

        [JsonPropertyName("invoicePdf")]
        public byte[]? InvoicePdf { get; set; }

        [JsonPropertyName("emailInvoicePdf")]
        public byte[]? EmailInvoicePdf { get; set; }

        [JsonPropertyName("sourceInvoıceNumber")]
        public string? SourceInvoıceNumber { get; set; }

        [JsonPropertyName("irbmValidationTime")]
        public DateTime? IrbmValidationTime { get; set; }

        [JsonPropertyName("validationDate")]
        public DateTime? ValidationDate { get; set; }

        [JsonPropertyName("cbcBaseQuantity")]
        public string? CbcBaseQuantity { get; set; }

        [JsonPropertyName("originalInvoiceNumber")]
        public string? OriginalInvoiceNumber { get; set; }
    }

    public class LineItemDB
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("lineId")]
        public int? LineId { get; set; }

        [JsonPropertyName("invoiceId")]
        public int? InvoiceId { get; set; }

        [JsonPropertyName("cbcIDVATcategoryCode")]
        public string? CbcIDVATcategoryCode { get; set; }

        [JsonPropertyName("productId")]
        public string? ProductId { get; set; }

        [JsonPropertyName("cbcIDItemCountryOfOrigin")]
        public string? CbcIDItemCountryOfOrigin { get; set; }

        [JsonPropertyName("cbcDescription")]
        public string? CbcDescription { get; set; }

        [JsonPropertyName("cbcDescriptionCode")]
        public string? CbcDescriptionCode { get; set; }

        [JsonPropertyName("cbcBaseQuantity")]
        public string? CbcBaseQuantity { get; set; }

        [JsonPropertyName("cbcBaseAmount")]
        public decimal? CbcBaseAmount { get; set; }

        [JsonPropertyName("cbcAmount")]
        public decimal? CbcAmount { get; set; }

        [JsonPropertyName("cbcDiscountRate")]
        public decimal? CbcDiscountRate { get; set; }

        [JsonPropertyName("cbcDiscountAmount")]
        public decimal? CbcDiscountAmount { get; set; }

        [JsonPropertyName("cbcTaxType")]
        public string? CbcTaxType { get; set; }

        [JsonPropertyName("cbcTaxRate")]
        public decimal? CbcTaxRate { get; set; }

        [JsonPropertyName("cbcTaxAmount")]
        public decimal? CbcTaxAmount { get; set; }

        [JsonPropertyName("cbcMeasure")]
        public string? CbcMeasure { get; set; }

        [JsonPropertyName("cbcAllowanceType")]
        public string? CbcAllowanceType { get; set; }

        [JsonPropertyName("cbcAllowanceReasonCode")]
        public string? CbcAllowanceReasonCode { get; set; }

        [JsonPropertyName("cbcAllowanceText")]
        public string? CbcAllowanceText { get; set; }

        [JsonPropertyName("cbcAllowanceBaseAmount")]
        public decimal? CbcAllowanceBaseAmount { get; set; }

        [JsonPropertyName("cbcAllowanceMultiplierFactor")]
        public decimal? CbcAllowanceMultiplierFactor { get; set; }

        [JsonPropertyName("cbcAllowanceAmount")]
        public decimal? CbcAllowanceAmount { get; set; }

        [JsonPropertyName("cbcChargeType")]
        public string? CbcChargeType { get; set; }

        [JsonPropertyName("cbcChargeReasonCode")]
        public string? CbcChargeReasonCode { get; set; }

        [JsonPropertyName("cbcChargeText")]
        public string? CbcChargeText { get; set; }

        [JsonPropertyName("cbcChargeBaseAmount")]
        public decimal? CbcChargeBaseAmount { get; set; }

        [JsonPropertyName("cbcChargeMultiplierFactor")]
        public decimal? CbcChargeMultiplierFactor { get; set; }

        [JsonPropertyName("cbcChargeAmount")]
        public decimal? CbcChargeAmount { get; set; }

        [JsonPropertyName("cbcPrice")]
        public decimal? CbcPrice { get; set; }

        [JsonPropertyName("cbcTaxExemptionDetails")]
        public string? CbcTaxExemptionDetails { get; set; }

        [JsonPropertyName("cbcTaxExemptedAmount")]
        public decimal? CbcTaxExemptedAmount { get; set; }

        [JsonPropertyName("cbcTotalExcludingTax")]
        public decimal? CbcTotalExcludingTax { get; set; }

        [JsonPropertyName("cbcItemClassificationCode")]
        public string? CbcItemClassificationCode { get; set; }

        [JsonPropertyName("cbcProductTariffClass")]
        public string? CbcProductTariffClass { get; set; }

        [JsonPropertyName("cbcTaxschemeID")]
        public string? CbcTaxschemeID { get; set; }

        [JsonPropertyName("cbcTaxschemeAgencyID")]
        public string? CbcTaxschemeAgencyID { get; set; }

        [JsonPropertyName("cbcTaxschemeAgencyCode")]
        public string? CbcTaxschemeAgencyCode { get; set; }

        [JsonPropertyName("cbcInvoiceLineNetAmount")]
        public decimal? CbcInvoiceLineNetAmount { get; set; }

        [JsonPropertyName("cbcNetAmount")]
        public decimal? CbcNetAmount { get; set; }

        [JsonPropertyName("cbcItemClassificationClass")]
        public string? CbcItemClassificationClass { get; set; }

        [JsonPropertyName("cbcProductTariffCode")]
        public string? CbcProductTariffCode { get; set; }

        [JsonPropertyName("cbcSubtotal")]
        public decimal? CbcSubtotal { get; set; }

        [JsonPropertyName("cbcSSTTaxCategory")]
        public string? CbcSSTTaxCategory { get; set; }

        [JsonPropertyName("originCountry")]
        public string? OriginCountry { get; set; }
    }

    public class DocTaxSubTotal
    {
        [JsonPropertyName("documentSubTotalId")]
        public int? DocumentSubTotalId { get; set; }

        [JsonPropertyName("invoiceId")]
        public int? InvoiceId { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("categoryTotalLines")]
        public int? CategoryTotalLines { get; set; }

        [JsonPropertyName("categoryTaxCategory")]
        public string? CategoryTaxCategory { get; set; }

        [JsonPropertyName("categoryTaxableAmount")]
        public decimal? CategoryTaxableAmount { get; set; }

        [JsonPropertyName("categoryTaxAmount")]
        public decimal? CategoryTaxAmount { get; set; }

        [JsonPropertyName("categoryTaxRate")]
        public decimal CategoryTaxRate { get; set; }

        [JsonPropertyName("categoryTaxExemptionReason")]
        public string? CategoryTaxExemptionReason { get; set; }

        [JsonPropertyName("categoryTaxSchemeId")]
        public string? CategoryTaxSchemeId { get; set; }

        [JsonPropertyName("cbcTaxschemeAgencyID")]
        public string? CbcTaxschemeAgencyID { get; set; }

        [JsonPropertyName("cbcTaxschemeAgencyCode")]
        public string? CbcTaxschemeAgencyCode { get; set; }

        [JsonPropertyName("amountExemptedfromTax")]
        public decimal? AmountExemptedFromTax { get; set; }

        [JsonPropertyName("invoiceLineItemId")]
        public int? InvoiceLineItemId { get; set; }
    }
}
