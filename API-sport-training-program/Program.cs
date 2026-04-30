using API_sprot_training_program.Models;
using API_sprot_training_program.Services;
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


var connection_string_database = Environment.GetEnvironmentVariable("TrainingProgramsDatabase_connectionString");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connection_string_database));

builder.Services.AddSingleton<IDataBaseSettings>(
    new TraningsDataBaseSettings(
         Environment.GetEnvironmentVariable("Collectoin_coaches") ?? "coaches",
         Environment.GetEnvironmentVariable(" Collection_trainings") ?? "trainings",
         Environment.GetEnvironmentVariable("DB_name") ?? "db"
        )
    );

var connection_string_redis = Environment.GetEnvironmentVariable("Redis_connectionString");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connection_string_redis;

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