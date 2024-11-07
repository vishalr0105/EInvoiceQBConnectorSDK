using EInvoiceQuickBooks.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using Intuit.Ipp.OAuth2PlatformClient;
using EInvoiceQuickBooks.Models1;
using Intuit.Ipp.Core.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System;
using Intuit.Ipp.Exception;
using System.Net.Http;
using System.Xml.Serialization;
using System.ComponentModel.Design;

namespace EInvoiceQuickBooks.Services
{
    public class InvoiceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QuickBooksSettings _qBooksConfig;
        private readonly string realmId;
        private readonly string clientId;
        private readonly string clientKey;
        private readonly string refreshToken;

        public InvoiceService(IHttpClientFactory httpClientFactory, IOptions<QuickBooksSettings> quickBooksSettings)
        {
            _httpClientFactory = httpClientFactory;
            _qBooksConfig = quickBooksSettings.Value;
            realmId = _qBooksConfig.RealmId;
            clientId = _qBooksConfig.ClientId;
            clientKey = _qBooksConfig.ClientSecret;
            refreshToken = _qBooksConfig.RefreshToken;
        }

        public async Task<Invoice> GetInvoiceAsync(string invoiceId)
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

        public async Task<object> GetInvoicePDFAsync(string invoiceId)
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

        public async Task<Invoice> UpdateInvoice(Invoice invoiceToUpdate)
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

        public async Task<CreateOrUpdateInvoiceResponse> CreateOrUpdateInvoice(object invoiceUpdate)
        {
            try
            {
                var content = new StringContent(invoiceUpdate.ToString(), Encoding.UTF8, "application/json");
                var accessToken = await GetAccessToken();
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

        //Explicitly Send the Invoice Email
        public async Task<string> SendInvoiceEmailAsync(string invoiceId)
        {
            try
            {
                var accessToken = await GetAccessToken();
                HttpClient client = new HttpClient();

                var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/invoice/{invoiceId}/send";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Content-Type", "application/octet-stream");

                var content = new StringContent(string.Empty);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = content;

                // Send the request and get the response
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

        #region LHDN API's calling

        public async Task<string> LoginAsync(string loginId, string password, string domain)
        {
            var _httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://dev.advintek.com.my:743/api/2024.1/eInvoice/Login");
            request.Headers.Add("accept", "*/*");

            var content = new StringContent($"{{\n  \"loginId\": \"{loginId}\",\n  \"password\": \"{password}\",\n  \"domain\": \"{domain}\"\n}}", null, "application/json");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                var resp = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<SubmitDocumentResponse>(resp);
                var dataObject = JsonConvert.DeserializeObject<LoginData>(apiResponse?.Data?.ToString());
                var token = dataObject?.Token;
                return token;
            }
            return string.Empty;
        }

        public async Task<string> GetQuickBooksLoginDataAsync(string clientID, string clientKey, string userID)
        {
            try
            {
                var _httpClient = new HttpClient();
                var url = $"https://dev.advintek.com.my:743/api/2024.1/QB/LoginWithQB?ClientID={clientID}&ClientKey={clientKey}&UserID={userID}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("accept", "*/*");

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

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
                // Handle any other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<int> CheckAlreadyExists(string invoiceId, string token)
        {
            try
            {
                var _httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.advintek.com.my:743/api/2024.1/QB/GetInvoiceByInvoiceNumber?invoiceNumber={invoiceId}");
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

        public async Task<string> GetDBInvoice(string invoiceId, string token)
        {
            try
            {
                var _httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.advintek.com.my:743/api/2024.1/QB/GetInvoiceByInvoiceNumber?invoiceNumber={invoiceId}");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var document = JsonDocument.Parse(jsonString);

                    // Get the "data"-"invoice"-"quickBookDetails" field
                    var dataElement = document.RootElement.GetProperty("data");
                    var invoiceElement = dataElement.GetProperty("invoice");
                    var quickBookDetailsElement = invoiceElement.GetProperty("quickBookDetails");
                    var quickBookDetailsJsonString = quickBookDetailsElement.GetRawText();

                    var parsedObject = JsonConvert.DeserializeObject(quickBookDetailsJsonString);
                    string formattedJson = JsonConvert.SerializeObject(parsedObject, Formatting.Indented);

                    return formattedJson;
                }
                var errorContent = await response.Content.ReadAsStringAsync();

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task<string> SubmitInvoiceAsync(InvoiceRequest invoiceRequest, string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUrl = "https://dev.advintek.com.my:743/api/2024.1/eInvoice/eInvoiceCreateRequest";
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

                            if (dataObject?.AcceptedDocuments?.Count > 0)
                                return dataObject.AcceptedDocuments.FirstOrDefault()?.Uuid;

                            return resp; // Return raw JSON if accepted documents are empty
                        }
                    }

                    // Log or handle errors if status code is not successful
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

        public async Task<string> GetSubmitDocumentDetails(string uuid, string token)
        {
            try
            {
                var client = new HttpClient();

                var requestUrl = $"https://dev.advintek.com.my:743/api/LightWeight/GetSubmitDocumentDetails?uuid={uuid}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await client.SendAsync(request);

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> ProcessInvoiceMethod(ProcessRequest input, string token)
        {
            try
            {
                var client = new HttpClient();

                var requestUrl = "https://dev.advintek.com.my:743/api/2024.1/QB/SentPDFEmail";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var jsonContent = JsonConvert.SerializeObject(input);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                //response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

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
    }
}
