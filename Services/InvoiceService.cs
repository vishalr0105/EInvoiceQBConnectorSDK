using EInvoiceQuickBooks.Models;
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
using Serilog;

namespace EInvoiceQuickBooks.Services
{
    public class InvoiceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QuickBooksSettings _qBooksConfig;
        private readonly IConfiguration _configuration;
        private readonly string clientId;
        private readonly string clientKey;
        private readonly string lhdnBaseUrl;
        private readonly string environment;

        public InvoiceService(IHttpClientFactory httpClientFactory, IOptions<QuickBooksSettings> quickBooksSettings, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _qBooksConfig = quickBooksSettings.Value;
            clientId = _qBooksConfig.ClientId;
            clientKey = _qBooksConfig.ClientSecret;
            lhdnBaseUrl = _configuration["LHDNBaseUrl"];
            environment = _configuration["Environment"];
        }

        #region QB API's

        // Get Invoice from QB (SDK)
        public async Task<Invoice> GetInvoiceAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

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
                var jsonEx = JsonConvert.SerializeObject(ex);
                Log.Information($"{jsonEx}");
                throw;
            }
        }

        // Get PDF Invoice from QB (SDK)
        public async Task<object> GetInvoicePDFAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

                var oauthValidator = new OAuth2RequestValidator(accessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
                var dataService = new DataService(serviceContext);

                var pdfInvoice = dataService.GetPdf(new Invoice { Id = invoiceId });

                var base64String = Convert.ToBase64String(pdfInvoice);
                return base64String;
            }
            catch (Exception ex)
            {
                var jsonEx = JsonConvert.SerializeObject(ex);
                Log.Information($"{jsonEx}");
                return ex;
            }
        }

        // Update Invoice in QB (SDK)
        public async Task<Invoice> UpdateInvoice(Invoice invoiceToUpdate, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

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
                Log.Information($"Exception in UpdateInvoice - {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        // Either Create or Update Invoice in QB using jsonbody (No SDK)
        public async Task<CreateOrUpdateInvoiceResponse> CreateOrUpdateInvoice(object invoiceUpdate, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

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
                Log.Information($"{JsonConvert.SerializeObject(ex)}");
                return new CreateOrUpdateInvoiceResponse() { Status = "failure", Error = ex.Message, Data = null };
            }
        }

        // Explicitly Send the Invoice Email (No SDK)
        public async Task<string> SendInvoiceEmailAsync(string invoiceId, string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

                if (accessToken == null || accessToken.Contains("Error"))
                {
                    return "Refresh token expired, and unable to refresh it. Please update refresh token in configuration.";
                }

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
                Log.Information($"{JsonConvert.SerializeObject(ex)}");
                Console.WriteLine($"General error: {ex.Message}");
                return "failure";
            }
        }

        #region QB Token

        // Get Access Token using realmId from QB
        public async Task<string> GetAccessToken(string realmId)
        {
            try
            {
                var dbrefreshToken = await GetDbRefreshToken(realmId);
                var oauth2Client = new OAuth2Client(clientId, clientKey, "https://developer.intuit.com/v2/OAuth2Playground/RedirectUrl", environment);

                var tokenResp = await oauth2Client.RefreshTokenAsync(dbrefreshToken);
                if (tokenResp.IsError)
                {
                    if (tokenResp.Error == "invalid_grant")
                    {
                        Log.Information($"{tokenResp.Error}");
                        return $"Error - {tokenResp.Error}";
                    }
                }
                var data = tokenResp;

                if (!String.IsNullOrEmpty(data.Error) || String.IsNullOrEmpty(data.RefreshToken) || String.IsNullOrEmpty(data.AccessToken))
                {
                    throw new Exception("Refresh token failed - " + data.Error);
                }

                // If we've got a new refresh_token store it in the file
                if (dbrefreshToken != data.RefreshToken)
                {
                    Console.WriteLine("New Refresh Token Found: " + data.RefreshToken);
                }
                return data.AccessToken;
            }
            catch (Exception ex)
            {
                Log.Information($"Error in GetAccessToken - {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        #endregion

        #region QB Company Details

        // Get company info using realmId from QB
        public async Task<Company> GetCompanyInfo(string realmId)
        {
            try
            {
                var accessToken = await GetAccessToken(realmId);

                var oauthValidator = new OAuth2RequestValidator(accessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.IPS, oauthValidator);
                var dataService = new DataService(serviceContext);

                serviceContext.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                serviceContext.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;

                var company = dataService.FindById(new Company() { Id = realmId });
                return company;
            }
            catch (Exception ex)
            {
                Log.Information($"{JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        #endregion

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

        // Get QB Refresh Token(Previous) from DB
        public async Task<string> GetDbRefreshToken(string realmId)
        {
            try
            {
                string token = await GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);

                var _httpClient = new HttpClient();
                var url = $"{lhdnBaseUrl}/GetRefreshToken";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDocument = JsonDocument.Parse(content);
                    var root = jsonDocument.RootElement;
                    var refreshToken = root.GetProperty("data").GetProperty("refreshToken").GetString();
                    return refreshToken;
                }
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetDbRefreshToken: {JsonConvert.SerializeObject(ex)}");
                Log.Information($"Exception in GetDbRefreshToken: {JsonConvert.SerializeObject(ex)}");
                return "exception";
            }
        }

        // Get LHDN Company Information
        public async Task<LhdnCompany> GetLhdnCompanyInfo(string token)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{lhdnBaseUrl}/GetCompanyDetails");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var responsec = JsonConvert.DeserializeObject<SubmitDocumentResponse>(content);
                    if (responsec?.Data != null)
                    {
                        var lhdnCompany = JsonConvert.DeserializeObject<LhdnCompany>(responsec.Data.ToString()!);
                        return lhdnCompany;
                    }
                    else
                    {
                        throw new InvalidOperationException("Data is null or invalid.");
                    }
                    //return (LhdnCompany)responsec.Data;
                }
                Log.Information($"Error in GetLhdnCompanyInfo - {JsonConvert.SerializeObject(content)}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in GetLhdnCompanyInfo {JsonConvert.SerializeObject(ex)}");
                return null;
            }
        }

        // Get LHDN Participent Information
        public async Task<LhdnParticipant> GetCustomerDetails(string token, string emailAddress)
        {
            try
            {
                var client = new HttpClient();
                var url = $"{lhdnBaseUrl}/GetCustomerDetails?EmailAddress={emailAddress}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var responsec = JsonConvert.DeserializeObject<SubmitDocumentResponse>(content);
                    if (responsec?.Data != null)
                    {
                        var lhdnParticipent = JsonConvert.DeserializeObject<LhdnParticipant>(responsec.Data.ToString()!);
                        return lhdnParticipent;
                    }
                    else
                    {
                        throw new InvalidOperationException("Data is null or invalid.");
                    }
                    //return (LhdnParticipant)responsec.Data;
                }
                Log.Information($"Error in GetCustomerDetails - {content}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in GetCustomerDetails {ex}");
                return null;
            }
        }

        // Get Access Token for LHDN API's
        public async Task<string> GetQuickBooksLoginDataAsync(string clientID, string clientKey, string realmId)
        {
            try
            {
                var _httpClient = new HttpClient();

                var url = $"{lhdnBaseUrl}/LoginWithQB?ClientID={clientID}&ClientKey={clientKey}&RealmId={realmId}";
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
                Log.Information($"An error occurred: {ex.Message}");
                return ex.Message;
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
            catch (Exception)
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
                Log.Error($"Error in GetDBInvoice - {errorContent}");
                Console.WriteLine($"Error in GetDBInvoice - {errorContent}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Information($"An error occurred in GetDBInvoice : {ex}");
                Console.WriteLine($"An error occurred in GetDBInvoice : {ex}");
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
                    Log.Information($"Error Response: {errorResponse}");
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
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseContent;
                }
                Log.Error($"Error in ProcessInvoiceMethod (Email PdF Invoice) - {responseContent}");
                return responseContent;
            }
            catch (Exception ex)
            {
                Log.Information($"{JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        #endregion
    }
}
