using EInvoiceQuickBooks;
using EInvoiceQuickBooks.Models;
using EInvoiceQuickBooks.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

builder.Services.AddSingleton<IQueueService, InMemoryQueueService>();
builder.Services.AddHostedService<WebhookProcessingService>();
builder.Services.AddScoped<InvoiceService>();

builder.Services.AddHttpClient();

builder.Services.Configure<QuickBooksSettings>(builder.Configuration.GetSection("QuickBooksSettings"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = string.Empty; // Makes Swagger available at the root URL
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
