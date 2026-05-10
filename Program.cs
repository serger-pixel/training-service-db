using training_service_db.Models;
using training_service_db.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


var connection_string_database = Environment.GetEnvironmentVariable("db_connection_string");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connection_string_database));

builder.Services.AddSingleton<IDataBaseSettings>(
    new TraningsDataBaseSettings(
         Environment.GetEnvironmentVariable("redis_connection_string") ?? "coaches",
         Environment.GetEnvironmentVariable("collectoin_coaches") ?? "trainings",
         Environment.GetEnvironmentVariable("db_name") ?? "db"
        )
    );

var connection_string_redis = Environment.GetEnvironmentVariable("redis_connection_string") ??
    "redis:6379,password=password,abortConnect = false";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connection_string_redis ;

    options.InstanceName = "Api-Sport-Training";
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