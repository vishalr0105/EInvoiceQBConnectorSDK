namespace EInvoiceQuickBooks.Models
{
    public class LhdnCompany
    {
        public int? Id { get; set; }
        public int? CompanyId { get; set; }
        public string? ClassificationName { get; set; }
        public string? ClassificationCode { get; set; }
        public string? Tin { get; set; }
        public string? IdType { get; set; }
        public string? IdValue { get; set; }
        public string? PaymentMeansCode { get; set; }
        public string? DefaultPaymentTerms { get; set; }
        public string? RegistrationName { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? PostalZone { get; set; }
    }

    public class LhdnParticipant
    {
        public int? ParticipantId { get; set; }
        public string? ParticipantName { get; set; }
        public string? CountryCode { get; set; }
        public string? AdditionalInfo { get; set; }
        public string? RegnNo { get; set; }
        public string? Website { get; set; }
        public DateTime? DateAndTimeOfValidation { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? ParticipantStatus { get; set; }
        public int? TenantId { get; set; }
        public string? SstRegnNo { get; set; }
        public string? WorkflowStatus { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FullPeppolParticipantId { get; set; }
        public string? Tin { get; set; }
        public string? IndividualId { get; set; }
        public string? LegalEntityIdentifier { get; set; }
        public string? TourismTaxRegnNo { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public bool? IsBuyer { get; set; }
        public bool? IsSupplier { get; set; }
        public bool? IsBiller { get; set; }
        public string? CbcBnric { get; set; }
        public string? CbcBbrnNumber { get; set; }
        public string? CbcBCategory { get; set; }
        public int? AddressId { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? AddressType { get; set; }
        public string? PostalZone { get; set; }
    }
}
