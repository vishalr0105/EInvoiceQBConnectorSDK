using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Models1;
using Intuit.Ipp.Data;
using Intuit.Ipp.WebhooksService;
using Newtonsoft.Json;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Diagnostics;
using Serilog;

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

        public WebhookProcessingService(IQueueService queueService, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _queueService = queueService;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            dummyEmail = _configuration["DummyEmail"];
            clientId = _configuration["QuickBooksSettings:ClientId"];
            clientKey = _configuration["QuickBooksSettings:ClientSecret"];
        }

        //protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            if (_queueService.HasItems())
        //            {
        //                var payload = _queueService.Dequeue();
        //                if (payload != null)
        //                {
        //                    await ProcessInvoiceEmailedEventAsync(payload);
        //                }
        //            }
        //            else
        //            {
        //                // If no items, check the stopping token only when the queue is empty
        //                if (stoppingToken.IsCancellationRequested)
        //                {
        //                    Log.Information("Stopping requested and queue is empty. Exiting service.");
        //                }
        //                await System.Threading.Tasks.Task.Delay(1, stoppingToken);
        //            }
        //        }
        //        catch (TaskCanceledException ex)
        //        {
        //            // Handle the cancellation gracefully when no more items are available to process
        //            Log.Information($"Task was canceled due to stopping request.\n {ex}");
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error($"An error occurred while processing webhook: {ex.Message}");
        //        }
        //    }
        //    Log.Information("WebhookProcessingService has stopped.");
        //}

        // Recieve webhook event from queue

        private Timer? _timer;

        protected override System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initialize the timer to run the processing logic every 5 seconds.
            _timer = new Timer(async state => await RunAsync(stoppingToken), null,
                                TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task RunAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Process the queue items if available.
                if (_queueService.HasItems())
                {
                    var payload = _queueService.Dequeue();
                    if (payload != null)
                    {
                        await ProcessInvoiceEmailedEventAsync(payload);
                    }
                }
                else
                {
                    // Log if the queue is empty.
                    if (stoppingToken.IsCancellationRequested)
                    {
                        Log.Information("Stopping requested and queue is empty. Exiting service.");
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                // Handle the cancellation gracefully.
                Log.Information($"Task was canceled due to stopping request.\n {ex}");
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while processing webhook: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
        private async System.Threading.Tasks.Task ProcessInvoiceEmailedEventAsync(string payload)
        {
            // Create a new scope to resolve scoped services
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();

                    // Process the webhook payload
                    var obj = new WebhooksService();
                    var webhookEvents = obj.GetWebooksEvents(payload);

                    foreach (var webhookEventNotification in webhookEvents.EventNotifications)
                    {
                        foreach (var dataChangeEventEntity in webhookEventNotification.DataChangeEvent.Entities)
                        {
                            var invoiceId = dataChangeEventEntity.Id; 
                            var operation = dataChangeEventEntity.Operation;
                            var realmId = webhookEventNotification.RealmId;
                            var syncToken = "";

                            Console.WriteLine($"Processing {operation} operation on invoice with id: {invoiceId}");
                            Log.Information($"Processing {operation} operation on invoice with id: {invoiceId}");

                            string token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);

                            if (operation == "Emailed")
                            {
                                token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);
                                var check = await invoiceService.CheckAlreadyExists(invoiceId, token);
                                var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId, realmId);

                                if (originalInvoice != null)
                                {
                                    syncToken = originalInvoice.SyncToken.ToString();
                                    if (check == 0)
                                    {
                                        var res = await ProcessMethod(originalInvoice, invoiceId, check, realmId);

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
                                            var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);
                                            if (updateRes is Invoice && updateRes != null)
                                            {
                                                if (res == "success")
                                                {
                                                    Log.Information($"Invoice Resent successfully - {invoiceId}");
                                                    Console.WriteLine($"Invoice Resent successfully - {invoiceId}");
                                                }
                                                else if (res == "failure")
                                                {
                                                    Log.Information($"Invoice Resent failure - {invoiceId}");
                                                    Console.WriteLine($"Invoice Resent failure - {invoiceId}");
                                                }
                                                else
                                                {
                                                    Log.Information($"{res} - Invoice - {invoiceId}");
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
                                            var res = await ProcessMethod(originalInvoice, invoiceId, check, realmId);

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
                                                var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);
                                                if (updateRes is Invoice && updateRes != null)
                                                {
                                                    if (res == "success")
                                                    {
                                                        Log.Information($"Invoice Resent successfully - {invoiceId}");
                                                        Console.WriteLine($"Invoice Resent successfully - {invoiceId}");
                                                    }
                                                    else if (res == "failure")
                                                    {
                                                        Log.Information($"Invoice Resent failure - {invoiceId}");
                                                        Console.WriteLine($"Invoice Resent failure - {invoiceId}");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //if resend and updated code goes here
                                            var dbInvoice = await invoiceService.GetDBInvoice(invoiceId, token, Convert.ToInt32(syncToken), dummyEmail);

                                            var createRes = await invoiceService.CreateOrUpdateInvoice(dbInvoice, realmId);

                                            if (createRes.Status.ToLower() == "success")
                                            {
                                                var res = await ProcessMethod(originalInvoice, invoiceId, check, realmId);

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
                                                    var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);
                                                }

                                                //Log success
                                                Console.WriteLine($"Cannot Update invoice once sent to Tax Office. Reverted changes back in Invoice. And Sent Email.");
                                                Log.Information($"Cannot Update invoice once sent to Tax Office. Reverted changes back in Invoice. And Sent Email");
                                            }
                                        }
                                    }
                                }
                            }
                            else if (operation == "Delete")
                            {
                                //token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);
                                var checkExists = await invoiceService.CheckAlreadyExists(invoiceId, token);

                                if (checkExists == 1)
                                {
                                    try
                                    {
                                        token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);
                                        var dbInvoice = await invoiceService.GetDBInvoice(invoiceId, token, -1, dummyEmail);

                                        var createRes = await invoiceService.CreateOrUpdateInvoice(dbInvoice, realmId);
                                        if (createRes.Status.ToLower() == "success")
                                        {
                                            var sendEmailRes = await invoiceService.SendInvoiceEmailAsync(createRes.Data.Id, realmId);

                                            Console.WriteLine($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                                            Log.Information($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Exception in Delete-Create-Back Invoice - {invoiceId}.");
                                        Log.Information($"Exception in Delete-Create-Back Invoice - {invoiceId}.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Deleted invoice successfully, as it was not sent to Tax Office.");
                                    Log.Information($"Deleted invoice successfully, as it was not sent to Tax Office.");
                                }
                            }
                            else if (operation == "Create")
                            {
                                var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId, realmId);
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
                                            AnyIntuitObject = !String.IsNullOrEmpty(originalInvoice.BillEmail?.Address) ?originalInvoice.BillEmail?.Address : dummyEmail,
                                        }
                                    }.ToArray(),
                                        CustomerRef = originalInvoice.CustomerRef,
                                        Line = originalInvoice.Line
                                    };
                                    var resUpdateEmail = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);
                                }

                                Console.WriteLine($"Updated Email-field for newly created invoice - {invoiceId}.");
                                Log.Information($"Updated Email-field for newly created invoice - {invoiceId}.");
                            }
                            else if (operation == "Update")
                            {
                                var originalInvoice = await invoiceService.GetInvoiceAsync(invoiceId, realmId);

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
                                        var resUpdateEmail = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);

                                        Console.WriteLine($"Found an attempt to update Email-field, Reverted back for Invoice - {invoiceId}");
                                        Log.Information($"Found an attempt to update Email-field, Reverted back for Invoice - {invoiceId}");
                                    }
                                }
                            }

                            Console.WriteLine($"Invoice {invoiceId} processed successfully for operation {operation}.");
                            Log.Information($"Invoice {invoiceId} processed successfully for operation {operation}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Information($"An error occured in ProcessInvoiceEmailedEventAsync :{ex}");
                throw ex;
            }
        }

        #region Process

        private async Task<string> ProcessMethod(Invoice originalInvoice, string invoiceId, int isResend, string realmId)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();

                    var base64PdfString = await invoiceService.GetInvoicePDFAsync(invoiceId, realmId);

                    var originalEmail = originalInvoice.CustomField.Where(c => c.DefinitionId == "1" && c.AnyIntuitObject != null).Select(c => c.AnyIntuitObject.ToString()).FirstOrDefault();
                    var requestProgress = new ProcessRequest()
                    {
                        base64Pdf = base64PdfString.ToString(),
                        emailAddress = originalEmail == null ? dummyEmail : originalEmail,
                    };

                    var tokenResp = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);
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
                        var company = await invoiceService.GetCompanyInfo(realmId);
                        var req = GetBaseInvoiceRequest(originalInvoice, company);
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
                            if (resProcessInvoice.Contains("Email sent successfully"))
                            {
                                return "success";
                            }
                            return "failure";
                        }
                        else if (submitResp.ToLower().Contains("validation error"))
                        {
                            return "validation error";
                        }
                        if (submitResp.Contains("\"statusCode\":400"))
                        {
                            return $"A LongId was not found for this UUID";
                        }

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

        #region LHDN eInvoice Create Request-Body Formation

        private InvoiceRequest GetBaseInvoiceRequest(Invoice invoice, Company company)
        {
            return new InvoiceRequest
            {
                eInvoiceVersion = "1.0",//static
                eInvoiceTypeCode = "01",//static
                eInvoiceCodeOrNumber = invoice.Id,
                SourceInvoiceNumber = invoice.Id,
                eInvoiceDate = DateTime.Now.ToString("yyyy-MM-dd"),//"2023-12-29",
                eInvoiceTime = DateTime.Now.ToString("HH:mm:ss") + "Z",//"03:16:56Z",
                InvoiceCurrencyCode = invoice.CurrencyRef.Value, //"MYR",
                //CurrencyExchangeRate = "1.00000", // No need
                //PaymentMode = "03",
                //PaymentTerms = "30 days from invoice date",
                //PaymentDueDate = invoice.DueDate.ToString("yyyy-MM-dd"),//"2024-01-28",
                BillReferenceNumber = "PO NO: 3261164188", //To cehck imp
                SellerBankAccountNumber = "MBBEMYKL#514356100499", //To cehck imp
                SellerName = company.CompanyName,  
                SellerTIN = "C26072927020", //To cehck imp
                SellerCategory = "BRN",   //To cehck
                SellerBusinessRegistrationNumber = "201901029037", //To cehck imp not static
                SellerSSTRegistrationNumber = "NA",  //To cehck imp not static
                SellerEmail = company.Email?.Address,//company
                SellerMalaysiaStandardIndustrialClassificationCode = "30910",  // from company
                SellerContactNumber = !string.IsNullOrEmpty(company.Mobile?.FreeFormNumber) ? company.Mobile.FreeFormNumber
                                    : !string.IsNullOrEmpty(company.PrimaryPhone?.FreeFormNumber) ? company.PrimaryPhone.FreeFormNumber : "",
                SellerAddressLine0 = company.LegalAddr?.Line1,
                SellerAddressLine1 = company.LegalAddr?.Line2,
                SellerAddressLine2 = company.LegalAddr?.Line3,
                SellerPostalZone = company.LegalAddr?.PostalCode,
                SellerCityName = company.LegalAddr?.City,
                SellerState = "14", //company.LegalAddr?.CountrySubDivisionCode ?? " ", //To check from company
                SellerCountry = "MYS",//company.Country ?? "MYS",    //To check from company
                SellerBusinessActivityDescription = "MEDICAL LABORATORIES",  //To check from company not required 
                SellerMSIC = "46201", // to check imp
                BuyerName = invoice.CustomerRef.Value,
                BuyerTIN = "C20307408040",  // to check imp
                BuyerCategory = "BRN",  // to check imp
                BuyerBusinessRegistrationNumber = "200601028904",   // to check imp
                BuyerIdentificationNumberOrPassportNumber = null,   // to check imp
                BuyerSSTRegistrationNumber = "B10-1808-22000011",   // to check imp
                BuyerEmail = invoice.CustomField.Where(c => c.DefinitionId == "1" && c.AnyIntuitObject != null).Select(c => c.AnyIntuitObject.ToString()).FirstOrDefault() ?? dummyEmail,
                BuyerContactNumber = "16097995959", // to check imp not required but check
                BuyerAddressLine0 = !string.IsNullOrEmpty(invoice.BillAddr.Line1) ? invoice.BillAddr.Line1 : "Line 1",
                BuyerAddressLine1 = invoice.BillAddr?.Line2,
                BuyerAddressLine2 = invoice.BillAddr?.Line3,
                BuyerPostalZone = invoice.BillAddr?.PostalCode,
                BuyerCityName = invoice.BillAddr?.City,
                BuyerState = "14",//invoice.BillAddr?.CountrySubDivisionCode ?? // to check imp
                BuyerCountry = "MYS",//invoice.BillAddr?.Country ??     // to check imp

                SumOfInvoiceLineNetAmount = invoice.TotalAmt.ToString(), // to check imp
                SumOfAllowancesOnDocumentLevel = "0.00",        // to check imp
                TotalFeeOrChargeAmount = "0.00",
                TotalExcludingTax = invoice.TotalAmt.ToString("0.0") ?? "0.0",
                TotalIncludingTax = invoice.TotalAmt.ToString("0.0") ?? "0.0",
                RoundingAmount = "0.02",
                PaidAmount = "0.00",
                TotalPayableAmount = invoice.TotalAmt.ToString("0.0") ?? "0.0",
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
                TotalNetAmount = invoice.TotalAmt.ToString("0.0") ?? "0.0",
                InvoiceLine = GetLines(invoice),
                isPDF = false,
                OutputFormat = "json",
                SourceName = "Advintek_Aif",
                SourceFileName = "Advintek_Aif12",
                TaxOfficeSchedulerTemplateName = "Invoice Template",
                TemplateName = "PDF_Telis_inv",
                QuickBookDetails = GetQuickBookDetails(invoice),
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
                    ProductID = string.IsNullOrEmpty(line.DetailType.GetStringValue()) ? "Latex" : line.DetailType.GetStringValue(),
                    Description = String.IsNullOrEmpty(line.Description) ? "description" : line.Description,
                    ProductTariffCode = "4001.10.00",
                    ProductTariffClass = "PTC",
                    Country = "THA",
                    UnitPrice = line.Amount.ToString() ?? "0",
                    Quantity = "0",
                    Measurement = "WE",
                    Subtotal = invoice.TotalAmt.ToString() ?? "0",
                    SSTTaxCategory = null,
                    TaxType = "06",//invoice.TxnTaxDetail?.TxnTaxCodeRef?.Value ??
                    TaxRate = "0.0",
                    TaxAmount = invoice.TxnTaxDetail?.TotalTax.ToString() ?? "0",
                    DetailsOfTaxExemption = null,
                    AmountExemptedFromTax = null,
                    TotalExcludingTax = invoice.TotalAmt.ToString("0.00"),
                    InvoiceLineNetAmount = invoice.TotalAmt.ToString("0.00"),
                    NettAmount = invoice.TotalAmt.ToString("0.00"),
                    TaxCategorySchemeID = "UN/ECE 5153",
                    TaxCategorySchemeAgencyID = "6",
                    TaxCategorySchemeAgencyCode = "OTH"
                });
            }

            return res;
        }

        private object GetQuickBookDetails(Invoice invoice)
        {
            var result = new
            {
                CurrencyRef = invoice.CurrencyRef,
                Line = invoice.Line,
                CustomerRef = invoice.CustomerRef,
                CustomField = invoice.CustomField,
                BillEmail = invoice.BillEmail
            };

            // Convert the result to a minified JSON string
            string jsonString = JsonConvert.SerializeObject(result, Formatting.None);

            return jsonString;
        }

        #endregion
    }
}
