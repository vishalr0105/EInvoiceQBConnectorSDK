using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace EInvoiceQuickBooks.Models
{
    public class InvoiceDTO
    {
        [Key]
        //public int? DBId { get; set; }
        public string? Id { get; set; }
        public string? SyncToken { get; set; }
        public string? domain { get; set; }
        public string? sparse { get; set; }
        public string? DocNumber { get; set; }
        public string? TxnDate { get; set; }
        public string? DueDate { get; set; }
        public string? TotalAmt { get; set; }
        public string? Balance { get; set; }
        public string? MetaData { get; set; }
        public string? CustomField { get; set; }
        public string? CurrencyRef { get; set; }
        public string? CustomerMemo { get; set; }
        public string? SalesTermRef { get; set; }
        public string? ApplyTaxAfterDiscount { get; set; }
        public string? PrintStatus { get; set; }
        public string? EmailStatus { get; set; }
        public string? BillEmail { get; set; }
        public string? DeliveryInfo { get; set; }
        public string? AllowIPNPayment { get; set; }
        public string? AllowOnlinePayment { get; set; }
        public string? AllowOnlineCreditCardPayment { get; set; }
        public string? AllowOnlineACHPayment { get; set; }
        public string? EInvoiceStatus { get; set; }
        public string? Line { get; set; }
        public string? TxnTaxDetail { get; set; }
        public string? CustomerRef { get; set; }
        public string? BillAddr { get; set; }
        public string? ShipAddr { get; set; }
    }

    #region map dto
    public class InvoiceDTOMap
    {
        //[Key]
        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("syncToken")]
        public string? SyncToken { get; set; }

        [JsonProperty("domain")]
        public string? Domain { get; set; }

        [JsonProperty("sparse")]
        public bool? Sparse { get; set; }

        [JsonProperty("DocNumber")]
        public string? DocNumber { get; set; }

        [JsonProperty("TxnDate")]
        public DateTime? TxnDate { get; set; }

        [JsonProperty("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonProperty("Balance")]
        public decimal? Balance { get; set; }

        [JsonProperty("MetaData")]
        public object? MetaData { get; set; }  // Define the type based on your requirements

        [JsonProperty("CustomField")]
        public List<CustomField>? CustomField { get; set; }

        [JsonProperty("CurrencyRef")]
        public CurrencyRef? CurrencyRef { get; set; }

        [JsonProperty("CustomerMemo")]
        public string? CustomerMemo { get; set; }

        [JsonProperty("SalesTermRef")]
        public SalesTermRef? SalesTermRef { get; set; }

        [JsonProperty("ApplyTaxAfterDiscount")]
        public bool? ApplyTaxAfterDiscount { get; set; }

        [JsonProperty("PrintStatus")]
        public string? PrintStatus { get; set; }

        [JsonProperty("EmailStatus")]
        public string? EmailStatus { get; set; }

        [JsonProperty("BillEmail")]
        public BillEmail? BillEmail { get; set; }

        [JsonProperty("DeliveryInfo")]
        public DeliveryInfo? DeliveryInfo { get; set; }

        [JsonProperty("AllowIPNPayment")]
        public bool? AllowIPNPayment { get; set; }

        [JsonProperty("AllowOnlinePayment")]
        public bool? AllowOnlinePayment { get; set; }

        [JsonProperty("AllowOnlineCreditCardPayment")]
        public bool? AllowOnlineCreditCardPayment { get; set; }

        [JsonProperty("AllowOnlineACHPayment")]
        public bool? AllowOnlineACHPayment { get; set; }

        [JsonProperty("EInvoiceStatus")]
        public string? EInvoiceStatus { get; set; }

        [JsonProperty("Line")]
        public List<LineItem>? Line { get; set; }

        [JsonProperty("TxnTaxDetail")]
        public TxnTaxDetail? TxnTaxDetail { get; set; }

        [JsonProperty("CustomerRef")]
        public CustomerRef? CustomerRef { get; set; }

        [JsonProperty("BillAddr")]
        public Address? BillAddr { get; set; }

        [JsonProperty("ShipAddr")]
        public Address? ShipAddr { get; set; }
    }

    // Supporting classes for complex types
    public class MetaData
    {
        public DateTime? CreateTime { get; set; }
        public DateTime? LastUpdatedTime { get; set; }

        public override string ToString()
        {
            return $"CreateTime: {CreateTime}, LastUpdatedTime: {LastUpdatedTime}";
        }
    }

    public class CustomField
    {
        public int? DefinitionId { get; set; }
        public string? Name { get; set; }
        public string? StringValue { get; set; }
        public string? Type { get; set; }
    }

    public class CurrencyRef
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }
    //{
    //    public string? name { get; set; }
    //    public string? value { get; set; }
    //}

    public class CustomerMemo
    {
        public string? value { get; set; }
    }

    public class SalesTermRef
    {
        public string? name { get; set; }
        public string? value { get; set; }
    }

    //public class CustomerRef
    //{
    //    [JsonProperty("@name")]
    //    public string? name { get; set; }
    //    [JsonProperty("#text")]
    //    public string? value { get; set; }
    //}
    public class CustomerRef
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }

    public class TempCustomerRef
    {
        [JsonProperty("@name")]
        public string? Name { get; set; }

        [JsonProperty("#text")]
        public string? Text { get; set; }
    }

    public class BillEmail
    {
        public string? Address { get; set; }
    }

    public class DeliveryInfo
    {
        public string? DeliveryType { get; set; }
        public DateTime? DeliveryTime { get; set; }
    }

    //[JsonConverter(typeof(SalesItemLineDetailConverter))]
    [JsonConverter(typeof(LineItemConverter))]
    public class LineItem
    {
        public decimal Amount { get; set; }
        public string? DetailType { get; set; }
        public string? Id { get; set; }
        public int? LineNum { get; set; }
        public string? Description { get; set; }

        [JsonProperty("SalesItemLineDetail")]
        public SalesItemLineDetail? SalesItemLineDetail { get; set; }

        [JsonProperty("SubTotalLineDetail")]
        public SubTotalLineDetail? SubTotalLineDetail { get; set; }

        [JsonProperty("DiscountLineDetail")]
        public DiscountLineDetail? DiscountLineDetail { get; set; }
    }

    public class SalesItemLineDetail
    {
        [JsonProperty("ItemRef")]
        public ItemRef? ItemRef { get; set; }
        public string? Qty { get; set; }
        public string? UnitPrice { get; set; }

        //[JsonConverter(typeof(TaxCodeRefConverter))]
        public TaxCodeRef? TaxCodeRef { get; set; }
    }

    public class SubTotalLineDetail
    {
        // You can leave this empty or define any other relevant fields
    }

    public class DiscountLineDetail
    {
        public string? PercentBased { get; set; }
        public string? DiscountPercent { get; set; }
        [JsonProperty("DiscountAccountRef")]
        public DiscountAccountRef? DiscountAccountRef { get; set; }
    }

    public class DiscountAccountRef
    {
        [JsonProperty("@name")]
        public string? Name { get; set; }

        [JsonProperty("#text")]
        public string? Text { get; set; }
    }

    public class ItemRef
    {
        //[JsonProperty("@name")]
        public string? name { get; set; }

        //[JsonProperty("#text")]
        public string? value { get; set; }
    }

    public class TaxCodeRef
    {
        public string Value { get; set; }
    }
    //public class TaxCodeRef
    //{
    //    public string? Value { get; set; }

    //    [JsonProperty("@name")]
    //    public string? Name { get; set; }

    //    [JsonProperty("#text")]
    //    public string? Text { get; set; }
    //}

    public class TxnTaxDetail
    {
        public decimal? TotalTax { get; set; }
    }

    public class Address
    {
        public string? Id { get; set; }
        public string? Line1 { get; set; }
        public string? City { get; set; }
        public string? CountrySubDivisionCode { get; set; }
        public string? PostalCode { get; set; }
    }
    #endregion

    public class LineItemConverter : JsonConverter<LineItem>
    {
        public override LineItem ReadJson(JsonReader reader, Type objectType, LineItem existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            var lineItem = new LineItem
            {
                Amount = jsonObject["Amount"]?.ToObject<decimal>() ?? 0,
                DetailType = jsonObject["DetailType"]?.ToString(),
                Id = jsonObject["Id"]?.ToString(),
                LineNum = jsonObject["LineNum"]?.ToObject<int?>()
            };

            switch (lineItem.DetailType)
            {
                case "SalesItemLineDetail":
                    lineItem.SalesItemLineDetail = jsonObject["SalesItemLineDetail"]?.ToObject<SalesItemLineDetail>();

                    // Handle ItemRef manually if necessary
                    var itemRefJson = jsonObject["SalesItemLineDetail"]?["ItemRef"];
                    if (itemRefJson != null)
                    {
                        lineItem.SalesItemLineDetail.ItemRef = new ItemRef
                        {
                            name = (string)itemRefJson["@name"],
                            value = (string)itemRefJson["#text"]
                        };
                    }

                    // Handle TaxCodeRef manually
                    var taxCodeRefJson = jsonObject["SalesItemLineDetail"]?["TaxCodeRef"];
                    if (taxCodeRefJson != null)
                    {
                        lineItem.SalesItemLineDetail.TaxCodeRef = new TaxCodeRef
                        {
                            Value = (string)taxCodeRefJson["value"]
                        };
                    }
                    break;

                case "SubTotalLineDetail":
                    lineItem.SubTotalLineDetail = jsonObject["SubTotalLineDetail"]?.ToObject<SubTotalLineDetail>();
                    break;

                case "DiscountLineDetail":
                    lineItem.DiscountLineDetail = jsonObject["DiscountLineDetail"]?.ToObject<DiscountLineDetail>();
                    break;

                default:
                    throw new JsonSerializationException($"Unsupported DetailType: {lineItem.DetailType}");
            }

            return lineItem;
        }

        public override void WriteJson(JsonWriter writer, LineItem value, JsonSerializer serializer)
        {
            // Start writing the JSON object
            writer.WriteStartObject();

            // Write basic properties
            writer.WritePropertyName("Amount");
            writer.WriteValue(value.Amount);

            writer.WritePropertyName("DetailType");
            writer.WriteValue(value.DetailType);

            writer.WritePropertyName("Id");
            writer.WriteValue(value.Id);

            writer.WritePropertyName("LineNum");
            writer.WriteValue(value.LineNum);

            // Handle the detail type properties based on the DetailType field
            if (value.DetailType == "SalesItemLineDetail" && value.SalesItemLineDetail != null)
            {
                writer.WritePropertyName("SalesItemLineDetail");
                writer.WriteStartObject();

                // Serialize ItemRef
                if (value.SalesItemLineDetail.ItemRef != null)
                {
                    writer.WritePropertyName("ItemRef");
                    writer.WriteStartObject();
                    writer.WritePropertyName("name");
                    writer.WriteValue(value.SalesItemLineDetail.ItemRef.name);
                    writer.WritePropertyName("value");
                    writer.WriteValue(value.SalesItemLineDetail.ItemRef.value);
                    writer.WriteEndObject();
                }

                // Serialize TaxCodeRef
                if (value.SalesItemLineDetail.TaxCodeRef != null)
                {
                    writer.WritePropertyName("TaxCodeRef");
                    writer.WriteStartObject();
                    writer.WritePropertyName("value");
                    writer.WriteValue(value.SalesItemLineDetail.TaxCodeRef.Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
            else if (value.DetailType == "SubTotalLineDetail" && value.SubTotalLineDetail != null)
            {
                writer.WritePropertyName("SubTotalLineDetail");
                serializer.Serialize(writer, value.SubTotalLineDetail);
            }
            else if (value.DetailType == "DiscountLineDetail" && value.DiscountLineDetail != null)
            {
                writer.WritePropertyName("DiscountLineDetail");
                serializer.Serialize(writer, value.DiscountLineDetail);
            }

            // End the JSON object
            writer.WriteEndObject();
        }
    }
}
