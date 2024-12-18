using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Models1;
using Intuit.Ipp.Data;
using Intuit.Ipp.Exception;
using Intuit.Ipp.WebhooksService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                Log.Information($"Task was canceled due to stopping request.\n {JsonConvert.SerializeObject(ex)}");
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while processing webhook: {JsonConvert.SerializeObject(ex)}");
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

                                        if (res.Contains("Value cannot be null."))
                                        {
                                            Log.Error("Getting \"Value cannot be null.\" error in Process method.");
                                            return;
                                        }
                                        else if (res.ToLower().Contains("exception"))
                                        {
                                            Log.Error($"Getting Exception in Process method");
                                            return;
                                        }

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
                                                        DefinitionId = "2",
                                                        Name = "EInvoice Validation Status",
                                                        Type =CustomFieldTypeEnum.StringType,
                                                        AnyIntuitObject = statusToPass
                                                    },
                                                    new Intuit.Ipp.Data.CustomField
                                                    {
                                                        DefinitionId = "3",
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
                                                        DefinitionId = "2",
                                                        Name = "EInvoice Validation Status",
                                                        Type =CustomFieldTypeEnum.StringType,
                                                        AnyIntuitObject = "Invoice Sent Failure"
                                                    }
                                                }.ToArray(),
                                                CustomerRef = originalInvoice.CustomerRef,
                                                Line = originalInvoice.Line
                                            };
                                        }
                                        if (updateInvoiceInput != null && updateInvoiceInput.Id != null)
                                        {
                                            var updateRes = await invoiceService.UpdateInvoice(updateInvoiceInput, realmId);
                                            if (updateRes is Invoice && updateRes != null)
                                            {
                                                if (res == "success")
                                                {
                                                    Log.Information($"Invoice sent successfully - {invoiceId}");
                                                    Console.WriteLine($"Invoice sent successfully - {invoiceId}");
                                                }
                                                else if (res == "failure")
                                                {
                                                    Log.Information($"Invoice sent failure - {invoiceId}");
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
                                            if (res.Contains("Value cannot be null."))
                                            {
                                                Log.Error("Getting \"Value cannot be null.\" error in Process method.");
                                                return;
                                            }
                                            else if (res.ToLower().Contains("exception"))
                                            {
                                                Log.Error($"Getting Exception in Process method");
                                                return;
                                            }
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
                                                            DefinitionId = "2",
                                                            Name = "EInvoice Validation Status",
                                                            Type =CustomFieldTypeEnum.StringType,
                                                            AnyIntuitObject = "Invoice Resent Success"
                                                        },
                                                        new Intuit.Ipp.Data.CustomField
                                                        {
                                                            DefinitionId = "3",
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
                                                            DefinitionId = "2",
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

                                            if (createRes?.Status?.ToLower() == "success")
                                            {
                                                var res = await ProcessMethod(originalInvoice, invoiceId, check, realmId);
                                                if (res.Contains("Value cannot be null."))
                                                {
                                                    Log.Error("Getting \"Value cannot be null.\" error in Process method.");
                                                    return;
                                                }
                                                else if (res.ToLower().Contains("exception"))
                                                {
                                                    Log.Error($"Getting Exception in Process method");
                                                    return;
                                                }

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
                                                                DefinitionId = "2",
                                                                Name = "EInvoice Validation Status",
                                                                Type =CustomFieldTypeEnum.StringType,
                                                                AnyIntuitObject = "Invoice Resent Success"
                                                            },
                                                            new Intuit.Ipp.Data.CustomField
                                                            {
                                                                DefinitionId = "3",
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
                                                                DefinitionId = "2",
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
                                var checkExists = await invoiceService.CheckAlreadyExists(invoiceId, token);

                                if (checkExists == 1)
                                {
                                    try
                                    {
                                        token = await invoiceService.GetQuickBooksLoginDataAsync(clientId, clientKey, realmId);
                                        var dbInvoice = await invoiceService.GetDBInvoice(invoiceId, token, -1, dummyEmail);

                                        var createRes = await invoiceService.CreateOrUpdateInvoice(dbInvoice, realmId);
                                        if (createRes?.Status?.ToLower() == "success")
                                        {
                                            var sendEmailRes = await invoiceService.SendInvoiceEmailAsync(createRes.Data.Id, realmId);

                                            Console.WriteLine($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                                            Log.Information($"Cannot Delete invoice once sent to Tax Office. Created back Invoice - {createRes.Data.Id}.");
                                        }
                                    }
                                    catch (Exception)
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
                                                DefinitionId = "2",
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
                                                    DefinitionId = "2",
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
                Log.Information($"An error occured in ProcessInvoiceEmailedEventAsync :{JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        #region Process

        private async Task<string> ProcessMethod(Invoice originalInvoice, string invoiceId, int isResend, string realmId)
        {
            //await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(30));
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
                        invoiceNo = invoiceId,
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

                        var lhdnCompany = await invoiceService.GetLhdnCompanyInfo(tokenResp);
                        var lhdnParticipent = await invoiceService.GetCustomerDetails(tokenResp, originalEmail);
                        if (lhdnCompany == null || lhdnParticipent == null)
                        {
                            Log.Information($"Exception in Getting LHDN CompanyInfo/CustomerDetails");
                            return "exception"; 
                        }

                        var req = GetBaseInvoiceRequest(originalInvoice, company, lhdnCompany, lhdnParticipent);

                        string submitResp;
                        var count = 0;

                        do
                        {
                            submitResp = await invoiceService.SubmitInvoiceAsync(req, tokenResp);
                            if (!submitResp.Contains("LHDN access token not found"))
                            {
                                count = 6;
                                var jsonrequest = JObject.FromObject(req);
                                var chkjson = jsonrequest.ToString();
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
                        else if (submitResp.Contains("\"statusCode\":400"))
                        {
                            return $"A LongId was not found for this UUID";
                        }
                        else if (submitResp.ToLower().Contains("exception"))
                        {
                            return "exception";
                        }
                        //var jsonrequest = JObject.FromObject(req);
                        //var chkjson = jsonrequest.ToString();
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
                Log.Error($"Exception in ProcessMethod - {JsonConvert.SerializeObject(ex)}");
                return "exception";
            }
        }

        #endregion

        #region LHDN eInvoice Create Request-Body Formation

        private InvoiceRequest GetBaseInvoiceRequest(Invoice invoice, Company company, LhdnCompany lhdnCompany, LhdnParticipant lhdnParticipent)
        {
            try
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
                    PaymentMode = lhdnCompany.PaymentMeansCode,
                    PaymentTerms = lhdnCompany.DefaultPaymentTerms,
                    //PaymentDueDate = invoice.DueDate.ToString("yyyy-MM-dd"), // No need
                    BillReferenceNumber = null,
                    SellerBankAccountNumber = null,
                    SellerName = company.CompanyName,
                    SellerTIN = lhdnCompany.Tin,
                    SellerCategory = lhdnCompany.IdType, //idType
                    SellerBusinessRegistrationNumber = lhdnCompany.IdType == "BRN" ? lhdnCompany.IdValue : null, //idValue 
                    SellerSSTRegistrationNumber = lhdnCompany.IdType == "SST" ? lhdnCompany.IdValue : null, //idValue 
                    SellerEmail = lhdnCompany.Email,
                    SellerMalaysiaStandardIndustrialClassificationCode = lhdnCompany.ClassificationCode,
                    SellerContactNumber = lhdnCompany.Telephone,
                    SellerAddressLine0 = lhdnCompany.AddressLine1,
                    SellerAddressLine1 = lhdnCompany.AddressLine2,
                    SellerAddressLine2 = lhdnCompany.AddressLine3,
                    SellerPostalZone = lhdnCompany.PostalZone,
                    SellerCityName = lhdnCompany.City,
                    SellerState = lhdnCompany.State,
                    SellerCountry = lhdnCompany.Country,
                    SellerBusinessActivityDescription = lhdnCompany.ClassificationName,
                    SellerMSIC = null,
                    BuyerName = lhdnParticipent.ParticipantName,
                    BuyerTIN = lhdnParticipent.Tin,
                    BuyerCategory = lhdnParticipent.CbcBCategory,
                    BuyerBusinessRegistrationNumber = lhdnParticipent.CbcBbrnNumber,
                    BuyerIdentificationNumberOrPassportNumber = null,
                    BuyerSSTRegistrationNumber = lhdnParticipent.SstRegnNo,
                    BuyerEmail = lhdnParticipent.Email,
                    BuyerContactNumber = lhdnParticipent.Phone,
                    BuyerAddressLine0 = lhdnParticipent.AddressLine1,
                    BuyerAddressLine1 = lhdnParticipent.AddressLine2,
                    BuyerAddressLine2 = lhdnParticipent.AddressLine3,
                    BuyerPostalZone = lhdnParticipent.PostalZone,
                    BuyerCityName = lhdnParticipent.City,
                    BuyerState = lhdnParticipent.State,
                    BuyerCountry = lhdnParticipent.Country,
                    SumOfInvoiceLineNetAmount = invoice.TotalAmt.ToString(), //without tax
                    SumOfAllowancesOnDocumentLevel = "0.00",
                    TotalFeeOrChargeAmount = "0.00",
                    TotalExcludingTax = invoice.TotalAmt.ToString("0.0") ?? "0.0", //SumOfInvoiceLineNetAmount
                    TotalIncludingTax = GetTotalIncludingTax(invoice), //invoice.TotalAmt.ToString("0.0") ?? "0.0", //SumOfInvoiceLineNetAmount + tax
                    RoundingAmount = "0.02",
                    PaidAmount = "0.00",
                    TotalPayableAmount = GetTotalIncludingTax(invoice), //invoice.TotalAmt.ToString("0.0") ?? "0.0", //GetTotalIncludingTax(TotalAmt)
                    ReferenceNumberOfCustomsFormNo1ID = null,
                    ReferenceNumberOfCustomsFormNo1DocumentType = null,
                    Incoterms = null, //"DDP",
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
                    TotalNetAmount = GetTotalIncludingTax(invoice), // invoice.TotalAmt.ToString("0.0") ?? "0.0", //TotalIncludingTax
                    InvoiceLine = GetLines(invoice, lhdnCompany),
                    isPDF = false,
                    OutputFormat = "json",
                    SourceName = "Advintek_Aif",
                    SourceFileName = "Advintek_Aif12",
                    TaxOfficeSchedulerTemplateName = null,
                    TemplateName = null,
                    QuickBookDetails = GetQuickBookDetails(invoice),
                    DocTaxTotal = GetDocTaxTotal(invoice),
                    AllowanceCharges = new List<AllowanceCharge>()
                };
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in ProcessMethod->GetBaseInvoiceRequest - {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        private DocTaxTotal GetDocTaxTotal(Invoice invoice)
        {
            if (invoice.TxnTaxDetail?.TaxLine == null)
            {
                return new DocTaxTotal
                {
                    TaxCategoryTaxAmountInAccountingCurrency = invoice.TotalAmt.ToString() ,
                    TotalTaxableAmountPerTaxType = invoice.TotalAmt.ToString(), //"1080.00",
                    TaxCategoryId =  "06", //"06",
                    TaxCategoryTaxSchemeId = "UN/ECE 5153",
                    TaxCategorySchemeAgencyID = "6",
                    TaxCategorySchemeAgencyCode = "OTH",
                    TaxCategoryRate = "0.0",
                    DetailsOfTaxExemption = ""
                };
            }
            var taxDetail = invoice.TxnTaxDetail?.TaxLine?.FirstOrDefault().AnyIntuitObject;
            string totalTaxableAmountPerTaxType = "";
            string taxCategoryId = "";
            decimal taxPercent = 0.0m;

            if (taxDetail is TaxLineDetail taxLineDetail)
            {
                // Extract UnitPrice if available
                if (taxLineDetail.NetAmountTaxable is decimal netAmountTaxable)
                {
                    totalTaxableAmountPerTaxType = netAmountTaxable.ToString();
                }

                if (taxLineDetail.TaxRateRef is ReferenceType taxRateRef)
                {
                    taxCategoryId = taxRateRef.Value;
                }
                if (taxLineDetail.TaxPercent is decimal taxpercent)
                {
                    taxPercent = taxpercent;
                }
            }
            var txnTaxCodeRef = GetTxnTaxCodeRef(invoice.TxnTaxDetail?.TxnTaxCodeRef);

            return new DocTaxTotal
            {
                TaxCategoryTaxAmountInAccountingCurrency = GetTaxCategoryTaxAmount(invoice.TotalAmt, taxPercent), //"80"
                TotalTaxableAmountPerTaxType = totalTaxableAmountPerTaxType, //"1080.00",
                TaxCategoryId = GetTaxCategoryId(txnTaxCodeRef) == null ? "06" : GetTaxCategoryId(txnTaxCodeRef), //"06",
                TaxCategoryTaxSchemeId = "UN/ECE 5153",
                TaxCategorySchemeAgencyID = "6",
                TaxCategorySchemeAgencyCode = "OTH",
                TaxCategoryRate = taxPercent == null ? "0.0" : taxPercent.ToString(),
                DetailsOfTaxExemption = ""
            };
        }

        private string GetTxnTaxCodeRef(ReferenceType txnTaxCodeRef)
        {
            #region guru@etaprise.com
            //switch (txnTaxCodeRef.Value)
            //{
            //    case "4":
            //        return "SST - Sales Tax";
            //    case "5":
            //        return "SST - Service Tax";
            //    case "6":
            //        return "SST - Tourism Tax";
            //    case "7":
            //        return "SST - High-Value Goods Tax - Inactive";
            //    case "8":
            //        return "SST - High-Value Goods Tax";
            //    case "9":
            //        return "SST - Sales Tax on Low Value Goods";
            //    case "10":
            //        return "SST - Not Applicable";
            //    case "E":
            //    case "11":
            //        return "SST - Tax exemption";
            //    default:
            //        return "NON";
            //}
            #endregion

            #region accounting@nexright.com
            // accounting@nexright.com
            //switch (txnTaxCodeRef.Value)
            //{
            //    case "4":
            //        return "SST - High-Value Goods Tax";
            //    case "5":
            //        return "SST - Not Applicable";
            //    case "6":
            //        return "SST - Sales Tax";
            //    case "7":
            //        return "SST - Sales Tax on Low Value Goods";
            //    case "8":
            //        return "SST - Service Tax";
            //    case "9":
            //        return "SST - Tax exemption";
            //    case "10":
            //        return "SST - Tourism Tax";
            //    case "E":
            //    case "11":
            //        return "SST - Tax exemption";
            //    default:
            //        return "NON";
            //}
            #endregion

            #region info@advintek.com
            // info@advintek.com

            switch (txnTaxCodeRef.Value)
            {
                case "4":
                    return "SST - High-Value Goods Tax";
                case "5":
                    return "SST - Not Applicable";
                case "6":
                    return "SST - Sales Tax";
                case "7":
                    return "SST - Sales Tax on Low Value Goods";
                case "8":
                    return "SST - Service Tax";
                case "9":
                    return "SST - Tax exemption";
                case "10":
                    return "SST - Tourism Tax";
                case "E":
                    return "SST - Tax exemption";
                default:
                    return "NON";
            }
            #endregion
        }

        private string GetTaxCategoryId(string value)
        {
            switch (value)
            {
                case "SST - High-Value Goods Tax":
                    return "04";
                case "SST - Not Applicable":
                    return "06";
                case "SST - Sales Tax":
                    return "01";
                case "SST - Sales Tax on Low Value Goods":
                    return "05";
                case "SST - Service Tax":
                    return "02";
                case "SST - Tax exemption":
                    return "E";
                case "SST - Tourism Tax":
                    return "03";
                default:
                    return "SST - Tax exemption"; // Handle unexpected input
            }
        }

        private string GetTaxCategoryTaxAmount(decimal totalAmt, decimal taxPercent = 1)
        {
            var res = totalAmt * (taxPercent / 100);
            return res.ToString();
        }

        private string GetTotalIncludingTax(Invoice invoice)
        {
            if (invoice.TxnTaxDetail?.TaxLine == null)
            {
                return invoice.TotalAmt.ToString();
            }
            var taxDetail = invoice.TxnTaxDetail?.TaxLine?.FirstOrDefault().AnyIntuitObject;
            string totalTaxableAmountPerTaxType = "";
            string taxCategoryId = "";
            decimal taxPercent = 0.0m;

            if (taxDetail is TaxLineDetail taxLineDetail)
            {
                // Extract UnitPrice if available
                if (taxLineDetail.NetAmountTaxable is decimal netAmountTaxable)
                {
                    totalTaxableAmountPerTaxType = netAmountTaxable.ToString();
                }

                if (taxLineDetail.TaxRateRef is ReferenceType taxRateRef)
                {
                    taxCategoryId = taxRateRef.Value;
                }
                if (taxLineDetail.TaxPercent is decimal taxpercent)
                {
                    taxPercent = taxpercent;
                }
            }

            var tax = invoice.TotalAmt * (taxPercent / 100);
            var totalAmtIncludingTax = invoice.TotalAmt + tax;

            return totalAmtIncludingTax.ToString();
        }

        private List<Models1.LineItem> GetLines(Invoice invoice, LhdnCompany lhdnCompany)
        {
            var lineCount = invoice.Line.Count();

            var res = new List<Models1.LineItem>();

            for (int i = 0; i < lineCount; i++)
            {
                var line = invoice.Line[i];

                string unitPrice = "0";
                string quantity = "0";
                if (line.DetailType != LineDetailTypeEnum.SubTotalLineDetail)
                {
                    if (line.AnyIntuitObject is SalesItemLineDetail salesItemLineDetail)
                    {
                        // Extract UnitPrice if available
                        unitPrice = salesItemLineDetail.AnyIntuitObject.ToString();

                        if (salesItemLineDetail.Qty is decimal Qty)
                        {
                            quantity = Qty.ToString();
                        }
                    }
                    var subtotal = (Convert.ToInt32(unitPrice) * Convert.ToInt32(quantity)).ToString();
                    if (line.AnyIntuitObject is not SubTotalLineDetail subTotalLineDetail)
                    {
                        res.Add(new Models1.LineItem
                        {
                            LineId = String.IsNullOrEmpty(line.Id) ? i.ToString() : line.Id,
                            ClassificationClass = "CLASS",
                            ClassificationCode = "022",
                            ProductID = line.Id,
                            Description = String.IsNullOrEmpty(line.Description) ? "description" : line.Description,
                            ProductTariffCode = null,
                            ProductTariffClass = null,
                            Country = lhdnCompany.Country,
                            UnitPrice = unitPrice,
                            Quantity = quantity,
                            Measurement = null, // Tocheck
                            Subtotal = subtotal, // invoice.TotalAmt.ToString() ?? "0", // UnitPrice * Quantity + TaxAmount
                            SSTTaxCategory = null,
                            TaxType = invoice.TxnTaxDetail?.TxnTaxCodeRef == null ? "06" : GetTaxTypeForLineItem(invoice.TxnTaxDetail?.TxnTaxCodeRef), // "06",// invoice.TxnTaxDetail?.TxnTaxCodeRef?.Value ??
                            TaxRate = "0.0",
                            TaxAmount = "0",
                            DetailsOfTaxExemption = null,
                            AmountExemptedFromTax = null,
                            TotalExcludingTax = GetTotalIncludingTax(invoice), // invoice.TotalAmt.ToString("0.00"), // Subtotal
                            InvoiceLineNetAmount = GetTotalIncludingTax(invoice), // invoice.TotalAmt.ToString("0.00"), // Subtotal + tax
                            NettAmount = GetTotalIncludingTax(invoice), // invoice.TotalAmt.ToString("0.00"), // Subtotal
                            TaxCategorySchemeID = "UN/ECE 5153",
                            TaxCategorySchemeAgencyID = "6",
                            TaxCategorySchemeAgencyCode = "OTH"
                        });
                    }
                }
            }
            return res;
        }

        private string GetTaxTypeForLineItem(ReferenceType txnTaxCodeRef)
        {
            if (txnTaxCodeRef.Value == null)
            {
                return "E";
            }
            switch (txnTaxCodeRef.Value)
            {
                case "4":
                    return "04"; // "SST - High-Value Goods Tax";
                case "5":
                    return "06"; // "SST - Not Applicable";
                case "6":
                    return "01"; // "SST - Sales Tax";
                case "7":
                    return "05"; // "SST - Sales Tax on Low Value Goods";
                case "8":
                    return "02"; // "SST - Service Tax";
                case "9":
                    return "E"; // "SST - Tax exemption";
                case "10":
                    return "03"; // "SST - Tourism Tax";
                case "E":
                    return "SST - Tax exemption";
                default:
                    return "NON";
            }
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
