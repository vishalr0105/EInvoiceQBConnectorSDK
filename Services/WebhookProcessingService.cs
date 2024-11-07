using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Models1;
using Intuit.Ipp.Data;
using Intuit.Ipp.WebhooksService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using Serilog;
using System.Net.WebSockets;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.Text;

namespace EInvoiceQuickBooks.Services
{
    public class WebhookProcessingService : BackgroundService
    {
        private readonly IQueueService _queueService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly string dummyEmail = "";
        private readonly string clientId = "";
        private readonly string clientKey = "";
        private readonly string realmeId = "";

        public WebhookProcessingService(IQueueService queueService, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _queueService = queueService;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            dummyEmail = _configuration["DummyEmail"];
            clientId = _configuration["QuickBooksSettings:ClientId"];
            clientKey = _configuration["QuickBooksSettings:ClientSecret"];
            realmeId = _configuration["QuickBooksSettings:RealmId"];
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_queueService.HasItems())
                    {
                        var payload = _queueService.Dequeue();
                        if (payload != null)
                        {
                            await ProcessInvoiceEmailedEventAsync(payload);
                        }
                    }

                    await System.Threading.Tasks.Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    LogError($"An error occurred while processing webhook: {ex.Message}");
                }
            }
        }

        private async System.Threading.Tasks.Task ProcessInvoiceEmailedEventAsync(string payload)
        {
            // Create a new scope to resolve scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();

                // Process the webhook payload
                var obj = new WebhooksService();
                var webhookEvents = obj.GetWebooksEvents(payload);

                var invoiceId = webhookEvents.EventNotifications.First().DataChangeEvent.Entities.FirstOrDefault()?.Id;
                var operation = webhookEvents.EventNotifications.First().DataChangeEvent.Entities.FirstOrDefault()?.Operation;
                var syncToken = "";

                Console.WriteLine($"Processing {operation} operation on invoice with id: {invoiceId}");
                LogInfo($"Processing {operation} operation on invoice with id: {invoiceId}");

                string token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmeId);

                if (operation == "Emailed")
                {
                    var check = await invoiceService.CheckAlreadyExists(invoiceId, token);
                    var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId);

                    if (originalInvoice != null)
                    {
                        syncToken = originalInvoice.SyncToken.ToString();
                        if (check == 0)
                        {
                            var res = await ProcessMethod(originalInvoice, invoiceId, check);

                            Invoice updateInvoiceInput = new Invoice();
                            if (res.ToLower() == "success" || res.Contains("A LongId was not found for this UUID"))
                            {
                                var statusToPass = res.ToLower() == "success" ? "Invoice Sent Success" : "Validation Pending";
                                updateInvoiceInput = new Invoice()
                                {
                                    Id = originalInvoice.Id,
                                    SyncToken = originalInvoice.SyncToken,
                                    sparse = true,
                                    BillEmail = originalInvoice.BillEmail,
                                    CustomField = new List<Intuit.Ipp.Data.CustomField>
                                    {
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "3",
                                            Name = "EInvoice Validation Status",
                                            Type =CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = statusToPass
                                        },
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "2",
                                            Name = "Invoice Sent SyncToken",
                                            Type =CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = (Convert.ToInt32(syncToken) + 1).ToString()
                                        }
                                    }.ToArray(),
                                    CustomerRef = originalInvoice.CustomerRef,
                                    Line = originalInvoice.Line
                                };
                            }
                            else if (res.ToLower() == "failure")
                            {
                                updateInvoiceInput = new Invoice()
                                {
                                    Id = originalInvoice.Id,
                                    SyncToken = originalInvoice.SyncToken,
                                    sparse = true,
                                    BillEmail = originalInvoice.BillEmail,
                                    CustomField = new List<Intuit.Ipp.Data.CustomField>
                                    {
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "3",
                                            Name = "EInvoice Validation Status",
                                            Type =CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = "Invoice Sent Failure"
                                        }
                                    }.ToArray(),
                                    CustomerRef = originalInvoice.CustomerRef,
                                    Line = originalInvoice.Line
                                };
                            }
                            if (updateInvoiceInput != null)
                            {
                                var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput);
                                if (updateRes is Invoice && updateRes != null)
                                {
                                    if (res == "success")
                                    {
                                        LogInfo($"Invoice Resent successfully - {invoiceId}");
                                        Console.WriteLine($"Invoice Resent successfully - {invoiceId}");
                                    }
                                    else if (res == "failure")
                                    {
                                        LogInfo($"Invoice Resent failure - {invoiceId}");
                                        Console.WriteLine($"Invoice Resent failure - {invoiceId}");
                                    }
                                    else
                                    {
                                        LogInfo($"{res} - Invoice - {invoiceId}");
                                        Console.WriteLine($"{res} - Invoice - {invoiceId}");
                                    }
                                }
                            }
                        }
                        else if (check == 1)
                        {
                            //If already sent
                            var lastSyncToken = originalInvoice.CustomField.Where(c => c.DefinitionId == "2").Select(c => c.AnyIntuitObject?.ToString()).FirstOrDefault();
                            var originalInvoiceSyncToken = originalInvoice.SyncToken.ToString();

                            if (String.IsNullOrEmpty(lastSyncToken))
                            {
                                lastSyncToken = originalInvoiceSyncToken;
                            }

                            if (originalInvoiceSyncToken == lastSyncToken)
                            {
                                var res = await ProcessMethod(originalInvoice, invoiceId, check);

                                Invoice updateInvoiceInput = new Invoice();

                                if (res.ToLower() == "success" || res.Contains("A LongId was not found for this UUID"))
                                {
                                    updateInvoiceInput = new Invoice()
                                    {
                                        Id = originalInvoice.Id,
                                        SyncToken = originalInvoice.SyncToken,
                                        sparse = true,
                                        BillEmail = originalInvoice.BillEmail,
                                        CustomField = new List<Intuit.Ipp.Data.CustomField>
                                        {
                                            new Intuit.Ipp.Data.CustomField
                                            {
                                                DefinitionId = "3",
                                                Name = "EInvoice Validation Status",
                                                Type =CustomFieldTypeEnum.StringType,
                                                AnyIntuitObject = "Invoice Resent Success"
                                            },
                                            new Intuit.Ipp.Data.CustomField
                                            {
                                                DefinitionId = "2",
                                                Name = "Invoice Sent SyncToken",
                                                Type =CustomFieldTypeEnum.StringType,
                                                AnyIntuitObject = (Convert.ToInt32(syncToken) + 1).ToString()
                                            }
                                        }.ToArray(),
                                        CustomerRef = originalInvoice.CustomerRef,
                                        Line = originalInvoice.Line
                                    };
                                }
                                else if (res.ToLower() == "failure")
                                {
                                    updateInvoiceInput = new Invoice()
                                    {
                                        Id = originalInvoice.Id,
                                        SyncToken = originalInvoice.SyncToken,
                                        sparse = true,
                                        BillEmail = originalInvoice.BillEmail,
                                        CustomField = new List<Intuit.Ipp.Data.CustomField>
                                        {
                                            new Intuit.Ipp.Data.CustomField
                                            {
                                                DefinitionId = "3",
                                                Name = "EInvoice Validation Status",
                                                Type =CustomFieldTypeEnum.StringType,
                                                AnyIntuitObject = "Invoice Resent Failure"
                                            }
                                        }.ToArray(),
                                        CustomerRef = originalInvoice.CustomerRef,
                                        Line = originalInvoice.Line
                                    };
                                }
                                if (updateInvoiceInput != null)
                                {
                                    var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput);
                                    if (updateRes is Invoice && updateRes != null)
                                    {
                                        if (res == "success")
                                        {
                                            LogInfo($"Invoice Resent successfully - {invoiceId}");
                                            Console.WriteLine($"Invoice Resent successfully - {invoiceId}");
                                        }
                                        else if (res == "failure")
                                        {
                                            LogInfo($"Invoice Resent failure - {invoiceId}");
                                            Console.WriteLine($"Invoice Resent failure - {invoiceId}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //if resend and updated code goes here
                                var dbInvoice = await invoiceService.GetDBInvoice(invoiceId, token);

                                var createRes = await invoiceService.CreateOrUpdateInvoice(dbInvoice);

                                if (createRes.Status.ToLower() == "success")
                                {
                                    var res = await ProcessMethod(originalInvoice, invoiceId, check);

                                    Invoice updateInvoiceInput = new Invoice();

                                    if (res.ToLower() == "success")
                                    {
                                        updateInvoiceInput = new Invoice()
                                        {
                                            Id = originalInvoice.Id,
                                            SyncToken = originalInvoice.SyncToken,
                                            sparse = true,
                                            BillEmail = originalInvoice.BillEmail,
                                            CustomField = new List<Intuit.Ipp.Data.CustomField>
                                            {
                                                new Intuit.Ipp.Data.CustomField
                                                {
                                                    DefinitionId = "3",
                                                    Name = "EInvoice Validation Status",
                                                    Type =CustomFieldTypeEnum.StringType,
                                                    AnyIntuitObject = "Invoice Resent Success"
                                                },
                                                new Intuit.Ipp.Data.CustomField
                                                {
                                                    DefinitionId = "2",
                                                    Name = "Invoice Sent SyncToken",
                                                    Type =CustomFieldTypeEnum.StringType,
                                                    AnyIntuitObject = (Convert.ToInt32(syncToken) + 1).ToString()
                                                }
                                            }.ToArray(),
                                            CustomerRef = originalInvoice.CustomerRef,
                                            Line = originalInvoice.Line
                                        };
                                    }
                                    else if (res.ToLower() == "failure")
                                    {
                                        updateInvoiceInput = new Invoice()
                                        {
                                            Id = originalInvoice.Id,
                                            SyncToken = originalInvoice.SyncToken,
                                            sparse = true,
                                            BillEmail = originalInvoice.BillEmail,
                                            CustomField = new List<Intuit.Ipp.Data.CustomField>
                                            {
                                                new Intuit.Ipp.Data.CustomField
                                                {
                                                    DefinitionId = "3",
                                                    Name = "EInvoice Validation Status",
                                                    Type =CustomFieldTypeEnum.StringType,
                                                    AnyIntuitObject = "Invoice Resent Failure"
                                                }
                                            }.ToArray(),
                                            CustomerRef = originalInvoice.CustomerRef,
                                            Line = originalInvoice.Line
                                        };
                                    }
                                    if (updateInvoiceInput != null)
                                    {
                                        var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput);
                                    }

                                    //Log success
                                    Console.WriteLine($"Cannot Update invoice once sent to Tax Office. Reverted changes back in Invoice. And Sent Email.");
                                    LogInfo($"Cannot Update invoice once sent to Tax Office. Reverted changes back in Invoice. And Sent Email");
                                }
                            }
                        }
                    }
                }
                else if (operation == "Delete")
                {
                    var checkExists = await invoiceService.CheckAlreadyExists(invoiceId, token);

                    if (checkExists == 1)
                    {
                        try
                        {
                            token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmeId);
                            var dbInvoice = await invoiceService.GetDBInvoice(invoiceId, token);

                            var createRes = await invoiceService.CreateOrUpdateInvoice(dbInvoice);
                            if (createRes.Status.ToLower() == "success")
                            {
                                var sendEmailRes = await invoiceService.SendInvoiceEmailAsync(createRes.Data.Id);
                                //Log success
                                Console.WriteLine($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                                LogInfo($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception in Delete-Create-Back Invoice - {invoiceId}.");
                            LogError($"Exception in Delete-Create-Back Invoice - {invoiceId}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Deleted invoice successfully, as it was not sent to Tax Office.");
                        LogInfo($"Deleted invoice successfully, as it was not sent to Tax Office.");
                    }
                }
                else if (operation == "Create")
                {
                    var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId);
                    if (originalInvoice != null)
                    {
                        var updateInvoiceInput = new Invoice()
                        {
                            Id = originalInvoice.Id,
                            SyncToken = originalInvoice.SyncToken,
                            sparse = true,
                            BillEmail = new EmailAddress()
                            {
                                DefaultSpecified = true,
                                Default = true,
                                Address = dummyEmail
                            },
                            CustomField = new List<Intuit.Ipp.Data.CustomField>
                                    {
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "3",
                                            Name = "EInvoice Validation Status",
                                            Type =CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = "Invoice Created"
                                        },
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "1",
                                            Name = "Original Email",
                                            Type = CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = originalInvoice.BillEmail?.Address ?? dummyEmail,
                                        }
                                    }.ToArray(),
                            CustomerRef = originalInvoice.CustomerRef,
                            Line = originalInvoice.Line
                        };
                        var resUpdateEmail = await invoiceService.UpdateInvoice(updateInvoiceInput);
                    }

                    Console.WriteLine($"Updated Email-field for newly created invoice - {invoiceId}.");
                    LogInfo($"Updated Email-field for newly created invoice - {invoiceId}.");
                }
                else if (operation == "Update")
                {
                    var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId);

                    var flag = false;
                    var emailCheck = originalInvoice.BillEmail;
                    if (emailCheck != null)
                    {
                        var email = emailCheck.Address.ToString();
                        if (!String.IsNullOrEmpty(email) && email == dummyEmail)
                        {
                            flag = true;
                        }
                        if (!flag)
                        {
                            var updateInvoiceInput = new Invoice()
                            {
                                Id = originalInvoice.Id,
                                SyncToken = originalInvoice.SyncToken,
                                sparse = true,
                                BillEmail = new EmailAddress()
                                {
                                    Address = dummyEmail
                                },
                                CustomField = new List<Intuit.Ipp.Data.CustomField>
                                {
                                        new Intuit.Ipp.Data.CustomField
                                        {
                                            DefinitionId = "3",
                                            Name = "EInvoice Validation Status",
                                            Type =CustomFieldTypeEnum.StringType,
                                            AnyIntuitObject = "Invoice Updated"
                                        }
                                }.ToArray(),
                                CustomerRef = originalInvoice.CustomerRef,
                                Line = originalInvoice.Line
                            };
                            var resUpdateEmail = await invoiceService.UpdateInvoice(updateInvoiceInput);

                            Console.WriteLine($"Found an attempt to update Email-field, Reverted back for Invoice - {invoiceId}");
                            LogInfo($"Found an attempt to update Email-field, Reverted back for Invoice - {invoiceId}");
                        }
                    }
                }

                Console.WriteLine($"Invoice {invoiceId} processed successfully for operation {operation}.");
                LogInfo($"Invoice {invoiceId} processed successfully for operation {operation}.");
            }
        }

        #region Process

        public async Task<string> ProcessMethod(Invoice originalInvoice, string invoiceId, int isResend)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();

                    var base64PdfString = await invoiceService.GetInvoicePDFAsync(invoiceId);

                    var originalEmail = originalInvoice.CustomField.Where(c => c.DefinitionId == "1" && c.AnyIntuitObject != null).Select(c => c.AnyIntuitObject.ToString()).FirstOrDefault();
                    var requestProgress = new ProcessRequest()
                    {
                        base64Pdf = base64PdfString.ToString(),
                        emailAddress = originalEmail == null ? dummyEmail : originalEmail,
                    };

                    var tokenResp = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmeId);
                    string resProcessInvoice = "";
                    if (isResend == 1)
                    {
                        resProcessInvoice = await invoiceService.ProcessInvoiceMethod(requestProgress, tokenResp);
                        if (resProcessInvoice.Contains("Email sent successfully"))
                        {
                            return "success";
                        }
                    }
                    else
                    {
                        var req = GetBaseInvoiceRequest(originalInvoice);
                        var json = JsonConvert.SerializeObject(req, Formatting.Indented);
                        string submitResp;
                        var count = 0;
                        do
                        {
                            submitResp = await invoiceService.SubmitInvoiceAsync(req, tokenResp);
                            if (!submitResp.Contains("LHDN access token not found"))
                            {
                                count = 6;
                            }
                            count++;
                        } while (submitResp.Contains("LHDN access token not found") || count < 5);

                        if (submitResp.Contains("e-Invoice Code or Number is already in use"))
                        {
                            resProcessInvoice = await invoiceService.ProcessInvoiceMethod(requestProgress, tokenResp);
                            return "success";
                        }
                        else if (submitResp.ToLower().Contains("validation error"))
                        {
                            return "validation error";
                        }
                        if (submitResp.Contains("\"statusCode\":400"))
                        {
                            return $"A LongId was not found for this UUID:G7VQQPMVY9KKGPB8WW7AKYBJ10";
                        }

                        var getSubmitDocDetailsResp = await invoiceService.GetSubmitDocumentDetails(submitResp, tokenResp);
                        resProcessInvoice = await invoiceService.ProcessInvoiceMethod(requestProgress, tokenResp);
                    }

                    if (resProcessInvoice.Contains("Email sent successfully"))
                    {
                        return "success";
                    }
                }

                return "failure";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region Request formation

        public InvoiceRequest GetBaseInvoiceRequest(Invoice invoice)
        {
            return new InvoiceRequest
            {
                eInvoiceVersion = "1.0",
                eInvoiceTypeCode = "01",
                eInvoiceCodeOrNumber = invoice.Id,
                SourceInvoiceNumber = "INV00000ALL",
                eInvoiceDate = "2023-12-29",
                eInvoiceTime = "03:16:56Z",
                InvoiceCurrencyCode = "MYR",
                CurrencyExchangeRate = "1.00000",
                PaymentMode = "03",
                PaymentTerms = "30 days from invoice date",
                PaymentDueDate = "2024-01-28",
                BillReferenceNumber = "PO NO: 3261164188",
                SellerBankAccountNumber = "MBBEMYKL#514356100499",
                SellerName = "Advintek Consulting Services Sdn. Bhd.",
                SellerTIN = "C26072927020",
                SellerCategory = "BRN",
                SellerBusinessRegistrationNumber = "201901029037",
                SellerSSTRegistrationNumber = "NA",
                SellerEmail = "info@advintek.com.my",
                SellerMalaysiaStandardIndustrialClassificationCode = "30910",
                SellerContactNumber = "+60122672127",
                SellerAddressLine0 = "Menara Centara,",
                SellerAddressLine1 = "Level 20 Unit 1,360,Jalan Tuanku Abdul",
                SellerAddressLine2 = "Rahman Kuala Lumpur 50100 Malaysia,",
                SellerPostalZone = "50100",
                SellerCityName = "Kuala Lumpur",
                SellerState = "01",
                SellerCountry = "MYS",
                SellerBusinessActivityDescription = "MEDICAL LABORATORIES",
                SellerMSIC = "46201",
                BuyerName = invoice.CustomerRef.Value, //"Nityo Infotech Services Sdn. Bhd.",
                BuyerTIN = "C20307408040",
                BuyerCategory = "BRN",
                BuyerBusinessRegistrationNumber = "200601028904",
                BuyerIdentificationNumberOrPassportNumber = null,
                BuyerSSTRegistrationNumber = "B10-1808-22000011",
                BuyerEmail = String.IsNullOrEmpty(invoice.BillEmail.Address) ? dummyEmail : invoice.BillEmail.Address,
                BuyerContactNumber = "16097995959",
                BuyerAddressLine0 = "Unit #35-01B , Q Sentral No.2A,",
                BuyerAddressLine1 = "Jalan Stesen Sentral 2,",
                BuyerAddressLine2 = "Kuala Lumpur Sentral,",
                BuyerPostalZone = "50470",
                BuyerCityName = "Kuala Lumpur",
                BuyerState = "14",
                BuyerCountry = "MYS",
                SumOfInvoiceLineNetAmount = invoice.TotalAmt.ToString(),
                SumOfAllowancesOnDocumentLevel = "0.00",
                TotalFeeOrChargeAmount = "0.00",
                TotalExcludingTax = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0.0" : invoice.TotalAmt.ToString(),
                TotalIncludingTax = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0.0" : invoice.TotalAmt.ToString(),
                RoundingAmount = "0.02",
                PaidAmount = "0.00",
                TotalPayableAmount = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0.0" : invoice.TotalAmt.ToString(),
                ReferenceNumberOfCustomsFormNo1ID = null,
                ReferenceNumberOfCustomsFormNo1DocumentType = null,
                Incoterms = "DDP",
                FreeTradeAgreementDocumentType = null,
                FreeTradeAgreementID = null,
                FreeTradeAgreementDocumentDescription = null,
                AuthorisationNumberForCertifiedExporter = null,
                AuthorisationNumberForCertifiedExporterAgencyName = null,
                ReferenceNumberOfCustomsFormNo2ID = null,
                ReferenceNumberOfCustomsFormNo2DocumentType = null,
                DetailsOfOtherChargesID = null,
                DetailsOfOtherChargesChargeIndicator = null,
                DetailsOfOtherChargesAmount = null,
                DetailsOfOtherChargesAllowanceChargeReason = null,
                TotalNetAmount = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0.0" : invoice.TotalAmt.ToString(),
                InvoiceLine = GetLines(invoice),
                isPDF = false,
                OutputFormat = "json",
                SourceName = "Advintek_Aif",
                SourceFileName = "Advintek_Aif12",
                TaxOfficeSchedulerTemplateName = "Invoice Template",
                TemplateName = "PDF_Telis_inv",
                quickBookDetails = GetQuickBookDetails(invoice),
                DocTaxTotal = new DocTaxTotal
                {
                    TaxCategoryTaxAmountInAccountingCurrency = "0.00",
                    TotalTaxableAmountPerTaxType = "0.00",
                    TaxCategoryId = "06",
                    TaxCategoryTaxSchemeId = "UN/ECE 5153",
                    TaxCategorySchemeAgencyID = "6",
                    TaxCategorySchemeAgencyCode = "OTH",
                    TaxCategoryRate = "0.0",
                    DetailsOfTaxExemption = ""
                },
                AllowanceCharges = new List<AllowanceCharge>()
            };
        }

        private List<Models1.LineItem> GetLines(Invoice invoice)
        {
            var lineCount = invoice.Line.Count();

            var res = new List<Models1.LineItem>();

            for (int i = 0; i < lineCount; i++)
            {
                var line = invoice.Line[i];

                res.Add(new Models1.LineItem
                {
                    LineId = String.IsNullOrEmpty(line.Id) ? i.ToString() : line.Id,
                    ClassificationClass = "CLASS",
                    ClassificationCode = "022",
                    ProductID = String.IsNullOrEmpty(line.DetailType.GetStringValue()) ? "Latex" : line.DetailType.GetStringValue(),
                    Description = String.IsNullOrEmpty(line.Description) ? "description" : line.Description,
                    ProductTariffCode = "4001.10.00",
                    ProductTariffClass = "PTC",
                    Country = "THA",
                    UnitPrice = String.IsNullOrEmpty(line.Amount.ToString()) ? "0" : line.Amount.ToString(),
                    Quantity = "0",
                    Measurement = "WE",
                    Subtotal = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0" : invoice.TotalAmt.ToString(),
                    SSTTaxCategory = null,
                    TaxType = invoice.TxnTaxDetail?.TxnTaxCodeRef?.Value ?? "NA",
                    TaxRate = "0.0",
                    TaxAmount = String.IsNullOrEmpty(invoice.TxnTaxDetail?.TotalTax.ToString()) ? "0" : invoice.TxnTaxDetail?.TotalTax.ToString(),
                    DetailsOfTaxExemption = null,
                    AmountExemptedFromTax = null,
                    TotalExcludingTax = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0" : invoice.TotalAmt.ToString(),
                    InvoiceLineNetAmount = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0" : invoice.TotalAmt.ToString(),
                    NettAmount = String.IsNullOrEmpty(invoice.TotalAmt.ToString()) ? "0" : invoice.TotalAmt.ToString(),
                    TaxCategorySchemeID = "UN/ECE 5153",
                    TaxCategorySchemeAgencyID = "6",
                    TaxCategorySchemeAgencyCode = "OTH"
                });
            }

            return res;
        }

        private string GetQuickBookDetails(Invoice invoice)
        {
            var result = new
            {
                CurrencyRef = invoice.CurrencyRef,
                Line = invoice.Line,
                CustomerRef = invoice.CustomerRef
            };

            // Convert the result to a minified JSON string
            string jsonString = JsonConvert.SerializeObject(result, Formatting.None);

            return jsonString;
        }
        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            string logFilePath = @"D:\Logs\WebHookServiceLogs.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - INFO - {message}");
            }
        }

        private void LogError(string message)
        {
            string logFilePath = @"D:\Logs\WebHookServiceLogs.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR - {message}");
            }
        }

        #endregion
    }
}
