using API_sprot_training_program.Models;
using API_sprot_training_program.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Renci.SshNet.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<DataBaseSettings>(
    builder.Configuration.GetSection("TrainingProgramsDatabase"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<DataBaseSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});


builder.Services.AddStackExchangeRedisCache(sp =>
{
    sp.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<DataBaseSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();

    return client.GetDatabase(settings.DatabaseName);
});

builder.Services.AddMetrics();

builder.Services.AddSingleton<TrainingService>();
builder.Services.AddSingleton<CoachService>();


builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("data_base_request_time")
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });



builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection(); 
app.UseAuthorization();    

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.Run();