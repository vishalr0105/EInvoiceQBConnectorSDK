using EInvoiceQuickBooks;
using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Services;
using Serilog.Events;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
             .Enrich.FromLogContext()
             .WriteTo.File("log/quickbookslog.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
             .CreateLogger();

// Add configuration with reloadOnChange enabled
//builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
//                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//                     .AddEnvironmentVariables();

//var configuration = new ConfigurationBuilder()
//    .SetBasePath(AppContext.BaseDirectory)
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .Build();
//var logDirectory = Path.Combine(AppContext.BaseDirectory, configuration["Logger:RequestLog:LogDirectory"]);
//if (!Directory.Exists(logDirectory))
//{
//    Directory.CreateDirectory(logDirectory);
//}

builder.Services.AddSingleton<IQueueService, InMemoryQueueService>();
builder.Services.AddHostedService<WebhookProcessingService>();
builder.Services.AddScoped<InvoiceService>();

builder.Services.AddHttpClient();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.Configure<QuickBooksSettings>(builder.Configuration.GetSection("QuickBooksSettings"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "QuickBooks WebHook", Version = "v1" });

    // Include only APIs tagged with "Webhook"
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.GroupName == "Webhook"; // Include endpoints grouped as "Webhook"
    });

    // Automatically group actions by GroupName property (from [ApiExplorerSettings] attribute)
    c.TagActionsBy(api => new[] { api.GroupName ?? "Default" });
});

var app = builder.Build();

// Use CORS policy
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBooks WebHook");
        c.RoutePrefix = string.Empty; // Makes Swagger available at the root URL
        //c.EnableDeepLinking();  // Allows to use specific endpoint
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();