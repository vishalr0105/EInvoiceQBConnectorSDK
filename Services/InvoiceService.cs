﻿using EInvoiceQuickBooks.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Security;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using Intuit.Ipp.OAuth2PlatformClient;
using EInvoiceQuickBooks.Models1;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Intuit.Ipp.Diagnostics;

namespace EInvoiceQuickBooks.Services
{
    public class InvoiceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QuickBooksSettings _qBooksConfig;
        private readonly IConfiguration _configuration;
        private readonly string clientId;
        private readonly string clientKey;
        private readonly string refreshToken;
        private readonly string lhdnBaseUrl;

        public InvoiceService(IHttpClientFactory httpClientFactory, IOptions<QuickBooksSettings> quickBooksSettings, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _qBooksConfig = quickBooksSettings.Value;
            clientId = _qBooksConfig.ClientId;
            clientKey = _qBooksConfig.ClientSecret;
            refreshToken = _qBooksConfig.RefreshToken;
            lhdnBaseUrl = _configuration["LHDNBaseUrl"];
        }

        #region QB API's

        // Get Invoice from QB (SDK)
        public async Task<Invoice> GetInvoiceAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken();

                var oauthValidator = new OAuth2RequestValidator(accessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
                var dataService = new DataService(serviceContext);

                serviceContext.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                serviceContext.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;

                var invoice = dataService.FindById(new Invoice { Id = invoiceId });

                return invoice;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Get PDF Invoice from QB (SDK)
        public async Task<object> GetInvoicePDFAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken();

                var oauthValidator = new OAuth2RequestValidator(accessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
                var dataService = new DataService(serviceContext);

                var pdfInvoice = dataService.GetPdf(new Invoice { Id = invoiceId });

                var base64String = Convert.ToBase64String(pdfInvoice);
                return base64String;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        // Update Invoice in QB (SDK)
        public async Task<Invoice> UpdateInvoice(Invoice invoiceToUpdate, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken();

                var oauthValidator = new OAuth2RequestValidator(accessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
                var dataService = new DataService(serviceContext);

                serviceContext.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                serviceContext.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;

                var updatedInvoice = dataService.Update(invoiceToUpdate);

                return updatedInvoice;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Either Create or Update Invoice in QB using jsonbody (No SDK)
        public async Task<CreateOrUpdateInvoiceResponse> CreateOrUpdateInvoice(object invoiceUpdate, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken();

                var content = new StringContent(invoiceUpdate.ToString(), Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                    var response = await httpClient.PostAsync($"{_qBooksConfig.BaseUrl}/v3/company/{realmId}/invoice?minorversion=73", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = JObject.Parse(responseBody);
                        var data = new CreateResponseToMapDB();
                        data.Id = responseJson["Invoice"]?["Id"]?.ToString();
                        data.SyncToken = responseJson["Invoice"]?["SyncToken"]?.ToString();
                        data.MetaData = responseJson["Invoice"]?["MetaData"]?.ToString();
                        data.CustomField = responseJson["Invoice"]?["CustomField"]?.ToString();
                        data.DocNumber = responseJson["Invoice"]?["DocNumber"]?.ToString();

                        return new CreateOrUpdateInvoiceResponse()
                        {
                            Status = "success",
                            Data = data,
                            Error = null
                        };
                    }
                    else
                    {
                        return new CreateOrUpdateInvoiceResponse() { Status = "failure", Error = responseBody, Data = null };
                    }
                }
            }
            catch (Exception ex)
            {
                return new CreateOrUpdateInvoiceResponse() { Status = "failure", Error = ex.Message, Data = null };
            }
        }

        // Explicitly Send the Invoice Email (No SDK)
        public async Task<string> SendInvoiceEmailAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken();

                HttpClient client = new HttpClient();

                var url = $"{_qBooksConfig.BaseUrl}/v3/company/{realmId}/invoice/{invoiceId}/send";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var content = new StringContent(string.Empty);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = content;

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Invoice sent successfully:");
                    return "success";
                }

                return "failure";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return "failure";
            }
        }

        #endregion

        #region QB Access/Refresh Token

        // Get Access Token from QB
        public async Task<string> GetAccessToken()
        {
            var oauth2Client = new OAuth2Client(clientId, clientKey, "https://developer.intuit.com/v2/OAuth2Playground/RedirectUrl", "production");

            var previousRefreshToken = refreshToken;
            var tokenResp = await oauth2Client.RefreshTokenAsync(previousRefreshToken);
            var data = tokenResp;

            if (!String.IsNullOrEmpty(data.Error) || String.IsNullOrEmpty(data.RefreshToken) || String.IsNullOrEmpty(data.AccessToken))
            {
                throw new Exception("Refresh token failed - " + data.Error);
            }

            // If we've got a new refresh_token store it in the file
            if (previousRefreshToken != data.RefreshToken)
            {
                Console.WriteLine("Writing new refresh token : " + data.RefreshToken);
                WriteNewRefreshTokenToWhereItIsStored(data.RefreshToken);
            }
            return data.AccessToken;
        }

        // Update Refresh Token in Configuration file
        private string WriteNewRefreshTokenToWhereItIsStored(string newRefreshToken)
        {
            string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var section = "QuickBooksSettings";
            var key = "RefreshToken";
            var newValue = newRefreshToken;

            var json = File.ReadAllText(_filePath);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[section][key] = newValue;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(_filePath, output);

            return "success";
        }

        #endregion

        #region Company Details

        // Get company info from realmId
        public async Task<Company> GetCompanyInfo(string realmId)
        {
            var accessToken = await GetAccessToken();

            var oauthValidator = new OAuth2RequestValidator(accessToken);
            var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
            var dataService = new DataService(serviceContext);

            serviceContext.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
            serviceContext.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;

            var company = dataService.FindById(new Company() { Id = realmId });
            return company;
        }

        #endregion

        #region Get Enum Values from QB

        // Get Detail type for Line item
        private LineDetailTypeEnum GetDetailTypeEnum(string detailType)
        {
            return detailType switch
            {
                "0" => LineDetailTypeEnum.PaymentLineDetail,
                "1" => LineDetailTypeEnum.DiscountLineDetail,
                "2" => LineDetailTypeEnum.TaxLineDetail,
                "3" => LineDetailTypeEnum.SalesItemLineDetail,
                "4" => LineDetailTypeEnum.ItemBasedExpenseLineDetail,
                "5" => LineDetailTypeEnum.AccountBasedExpenseLineDetail,
                "6" => LineDetailTypeEnum.DepositLineDetail,
                "7" => LineDetailTypeEnum.PurchaseOrderItemLineDetail,
                "8" => LineDetailTypeEnum.ItemReceiptLineDetail,
                "9" => LineDetailTypeEnum.JournalEntryLineDetail,
                "10" => LineDetailTypeEnum.GroupLineDetail,
                "11" => LineDetailTypeEnum.DescriptionOnly,
                "12" => LineDetailTypeEnum.SubTotalLineDetail,
                "13" => LineDetailTypeEnum.SalesOrderItemLineDetail,
                "14" => LineDetailTypeEnum.TDSLineDetail,
                "15" => LineDetailTypeEnum.ReimburseLineDetail,
                "16" => LineDetailTypeEnum.ItemAdjustmentLineDetail,
                _ => throw new ArgumentOutOfRangeException($"Unknown DetailType: {detailType}"),
            };
        }

        // Get Custom field type (e.g. StringType)
        private CustomFieldTypeEnum GetCustomFieldType(int type)
        {
            return type switch
            {
                0 => CustomFieldTypeEnum.StringType,
                1 => CustomFieldTypeEnum.BooleanType,
                2 => CustomFieldTypeEnum.NumberType,
                3 => CustomFieldTypeEnum.DateType,
                _ => throw new ArgumentOutOfRangeException($"Unknown DetailType: {type}"),
            };
        }

        // Get Custom field key (e.g. StringValue)
        private static string GetCustomFieldObjectKey(int typeValue)
        {
            return typeValue switch
            {
                0 => "StringValue",
                1 => "BooleanValue",
                2 => "NumberValue",
                3 => "DateValue",
                _ => "StringValue",
            };
        }

        #endregion

        #region LHDN API's calling

        // Get Access Token for LHDN API's
        public async Task<string> GetQuickBooksLoginDataAsync(string clientID, string clientKey, string realmId)
        {
            try
            {
                var company = await GetCompanyInfo(realmId);

                var companyEmail = company.Email.Address;

                var _httpClient = new HttpClient();
                var url = $"{lhdnBaseUrl}/LoginWithQB?ClientID={clientID}&ClientKey={clientKey}&EmailId={companyEmail}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("accept", "*/*");

                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDocument = JsonDocument.Parse(content);
                    var root = jsonDocument.RootElement;
                    var token = root.GetProperty("data").GetProperty("token").GetString();
                    return token;
                }
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }

        // Check Invoice already exists in DB
        public async Task<int> CheckAlreadyExists(string invoiceId, string token)
        {
            try
            {
                var _httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{lhdnBaseUrl}/GetInvoiceByInvoiceNumber?invoiceNumber={invoiceId}");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);

                var jsonString = await response.Content.ReadAsStringAsync();
                JObject jsonObj = JObject.Parse(jsonString);
                if (jsonObj != null && jsonObj["data"] != null && jsonObj["data"].Type == JTokenType.Object)
                {
                    var dataObj = jsonObj["data"] as JObject;

                    if (dataObj["invoice"] != null && !string.IsNullOrEmpty(dataObj["invoice"].ToString()))
                    {
                        return 1;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                return 3;
            }
        }

        // Get Invoice from DB
        public async Task<string> GetDBInvoice(string invoiceId, string token, int isCreateNew, string emailValue)
        {
            try
            {
                var _httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{lhdnBaseUrl}/GetInvoiceByInvoiceNumber?invoiceNumber={invoiceId}");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var document = JsonDocument.Parse(jsonString);

                    var dataElement = document.RootElement.GetProperty("data");
                    var invoiceElement = dataElement.GetProperty("invoice");
                    var quickBookDetailsElement = invoiceElement.GetProperty("quickBookDetails");
                    var quickBookDetailsJsonString = quickBookDetailsElement.GetRawText();

                    string formattedJson = Regex.Unescape(quickBookDetailsJsonString).Trim('"');

                    var jsonObject = JsonConvert.DeserializeObject<JObject>(formattedJson);
                    if (jsonObject["CurrencyRef"]?["Value"] != null)
                    {
                        jsonObject["CurrencyRef"]["value"] = jsonObject["CurrencyRef"]["Value"];

                        ((JObject)jsonObject["CurrencyRef"]).Property("Value")?.Remove();
                        ((JObject)jsonObject["CurrencyRef"]).Property("type")?.Remove();
                    }

                    if (jsonObject["CustomerRef"]?["Value"] != null)
                    {
                        jsonObject["CustomerRef"]["value"] = jsonObject["CustomerRef"]["Value"];

                        ((JObject)jsonObject["CustomerRef"]).Property("Value")?.Remove();
                        ((JObject)jsonObject["CustomerRef"]).Property("type")?.Remove();
                    }

                    var detailTypes = new[] { "PaymentLineDetail", "DiscountLineDetail", "TaxLineDetail", "SalesItemLineDetail",
                                                "ItemBasedExpenseLineDetail","AccountBasedExpenseLineDetail","DepositLineDetail",
                                                "PurchaseOrderItemLineDetail", "ItemReceiptLineDetail", "JournalEntryLineDetail",
                                                "GroupLineDetail", "DescriptionOnly","DescriptionLineDetail", "SubTotalLineDetail",
                                                "SalesOrderItemLineDetail", "TDSLineDetail", "ReimburseLineDetail",
                                                "ItemAdjustmentLineDetail"};

                    var propertiesToRemove = new[] { "AnyIntuitObject", "TaxClassificationRef", "UOMRef",
                                                      "PaymentLineEx", "DiscountLineDetailEx", "TaxLineDetailEx",
                                                      "SalesItemLineDetailEx", "ItemBasedExpenseLineDetailEx","ExpenseDetailLineDetailEx",
                                                      "DepositLineDetailEx", "PurchaseOrderItemLineDetailEx", "ItemReceiptLineDetailEx",
                                                      "JournalEntryLineDetailEx", "GroupLineDetailEx", "DescriptionLineDetailEx",
                                                      "ServiceDateSpecified", "ManuallyClosedSpecified", "TDSLineDetailEx" };

                    if (jsonObject["Line"] is JArray lineArray)
                    {
                        foreach (JObject lineItem in lineArray)
                        {
                            lineItem.Property("LinkedTxn")?.Remove();
                            lineItem.Property("CustomField")?.Remove();
                            lineItem.Property("LineEx")?.Remove();
                            lineItem.Property("ProjectRef")?.Remove();

                            if (lineItem is JObject line)
                            {
                                var propertiesToRemoveNulls = line.Properties()
                                              .Where(p => p.Value.Type == JTokenType.Null)
                                              .ToList();

                                foreach (var prop in propertiesToRemoveNulls)
                                {
                                    prop.Remove();
                                }

                                if (line["DetailType"] is JValue detailTypeValue)
                                {
                                    string detailTypeKey = detailTypeValue.ToString();
                                    string enumValue = GetDetailTypeEnum(detailTypeKey).ToString();
                                    line["DetailType"] = enumValue; // Update DetailType with enum string
                                }

                                if (line["AnyIntuitObject"] is JObject anyIntuitObject)
                                {
                                    var detailTypeValue1 = line["DetailType"]?.ToString();
                                    if (!string.IsNullOrEmpty(detailTypeValue1))
                                    {
                                        lineItem[detailTypeValue1] = anyIntuitObject;
                                    }
                                    line.Remove("AnyIntuitObject");
                                }

                                foreach (var detailType in detailTypes)
                                {
                                    if (line[detailType] is JObject detailObject)
                                    {
                                        // Remove specified properties from the current detail type object
                                        foreach (var prop in propertiesToRemove)
                                        {
                                            detailObject.Property(prop)?.Remove();
                                        }

                                        var propertiesToRemoveNullsFromDetail = detailObject.Properties()
                                                                     .Where(p => p.Value.Type == JTokenType.Null)
                                                                     .ToList();

                                        foreach (var prop in propertiesToRemoveNullsFromDetail)
                                        {
                                            prop.Remove();
                                        }

                                        var nestedPropertiesToModify = new[] { "ItemAccountRef", "TaxCodeRef", "ItemRef", "DiscountAccountRef" }; // Add more as needed

                                        foreach (var nestedProperty in nestedPropertiesToModify)
                                        {
                                            if (detailObject[nestedProperty] is JObject nestedObject)
                                            {
                                                // Remove the "type" property and replace "Value" with "value"
                                                nestedObject.Property("type")?.Remove();
                                                if (nestedObject["Value"] != null)
                                                {
                                                    nestedObject["value"] = nestedObject["Value"];
                                                    nestedObject.Property("Value")?.Remove();
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }

                    var filteredCustomFields = new JArray();
                    foreach (var field in jsonObject["CustomField"])
                    {
                        var definitionId = field["DefinitionId"]?.ToString();
                        if (definitionId != "2" && definitionId != "3")
                        {
                            int typeValue = field["Type"]?.ToObject<int>() ?? 0;
                            field["Type"] = GetCustomFieldType(typeValue).ToString();

                            var anyIntuitObjectValue = field["AnyIntuitObject"];
                            ((JObject)field).Property("AnyIntuitObject")?.Remove();
                            field[GetCustomFieldObjectKey(typeValue)] = anyIntuitObjectValue;

                            filteredCustomFields.Add(field);
                        }
                    }

                    jsonObject["CustomField"] = filteredCustomFields;

                    if (isCreateNew != -1)
                    {
                        jsonObject["Id"] = invoiceId;
                        jsonObject["SyncToken"] = isCreateNew.ToString();
                        jsonObject["sparse"] = true;
                    }
                    else
                    {
                        jsonObject["BillEmail"] = new JObject
                        {
                            ["Address"] = emailValue // Replace with dynamic value as needed
                        };
                    }
                    string prettyJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                    return prettyJson;
                }
                var errorContent = await response.Content.ReadAsStringAsync();

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        // Submit Invoice
        public async Task<string> SubmitInvoiceAsync(InvoiceRequest invoiceRequest, string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUrl = $"{lhdnBaseUrl}/eInvoiceCreateRequest";
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StringContent(
                            JsonConvert.SerializeObject(invoiceRequest),
                            Encoding.UTF8,
                            "application/json")
                    };

                    request.Headers.Add("accept", "*/*");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var resp = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<SubmitDocumentResponse>(resp);

                        if (apiResponse?.Data != null)
                        {
                            if (apiResponse.Data.ToString().Contains("Validation Error"))
                            {
                                return "validation error";
                            }
                            var dataObject = JsonConvert.DeserializeObject<Data>(apiResponse.Data.ToString());

                            if (dataObject != null)
                                return dataObject.Uuid;

                            return resp;
                        }
                    }

                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Response: {errorResponse}");
                    return errorResponse;
                }
            }
            catch (Exception)
            {
                return "exception";
            }
        }

        // Sent Invoice PDF Email
        public async Task<string> ProcessInvoiceMethod(ProcessRequest input, string token)
        {
            try
            {
                var client = new HttpClient();

                var requestUrl = $"{lhdnBaseUrl}/SentPDFEmail";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var jsonContent = JsonConvert.SerializeObject(input);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
