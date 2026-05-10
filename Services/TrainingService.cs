using API_sprot_training_program.Metrics;
using API_sprot_training_program.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Xml.Linq;

namespace API_sprot_training_program.Services
{
    public class TrainingService
    {
        private readonly IMongoCollection<Training> _programs;

        private readonly IMongoCollection<Coach> _coaches;

        private readonly IDistributedCache _cache;

        private readonly DataBaseRequestTime _data_base_metric;

        private const int LIMIT_OF_PROGRAMS = 1000;

        public TrainingService(
            IMongoClient mongoClient, 
            IDataBaseSettings settings,
            IMeterFactory meterFactory, 
            IDistributedCache cache)
            {

            var mongoDatabase = mongoClient.GetDatabase(
                settings.DatabaseName);

            _programs = mongoDatabase.GetCollection<Training>(
                settings.CollectionNameTraining);

            _coaches = mongoDatabase.GetCollection<Coach>(
                settings.CollectionNameCoach);

            Type type = typeof(Training);

            _data_base_metric = new DataBaseRequestTime(meterFactory);

            _cache = cache;

            Task.Run(() => UpdateCache(CancellationToken.None));

            string list_key = $"training_list";
            var db_programs = _programs.Find(_ => true).ToList<Training>();
            _cache.SetString(list_key, JsonSerializer.Serialize(db_programs));
        }

        private async Task UpdateCache(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromDays(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                string list_key = $"training_list";
                var db_programs = _programs.Find(_ => true).ToList<Training>();
                _cache.SetString(list_key, JsonSerializer.Serialize(db_programs));
            }
        }

        public async Task<List<TrainingOutput>> GetByFilter(String nameProperty, String value)
        {
            var property = typeof(TrainingOutput).GetProperty(nameProperty);

            if (property == null) return new List<TrainingOutput>();

            var targetType = property.PropertyType;
            var convertedValue = Convert.ChangeType(value, targetType);
            var filter = Builders<Training>.Filter.Eq(nameProperty, convertedValue);
            var programsList = _programs.Find(filter).ToListAsync();
            return programsList.Result.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<TrainingOutput>> GetRandomAsync(int count)
        {
            string list_key = $"training_list";
            var _random = new Random();
            List<Training>? cached_trainings = JsonSerializer.Deserialize<List<Training>>(_cache.GetString(list_key));

            return cached_trainings
            .OrderBy(x => _random.Next()) 
            .Take(count)
            .Select(
                element => MapToOutput(element)
                )
            .ToList();
        }

        public async Task<List<TrainingOutput>> GetOrderAsync()
        {
            string list_key = $"training_list";

            List<Training>? cached_trainings = JsonSerializer.Deserialize<List<Training>>(_cache.GetString(list_key));
            return cached_trainings.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<TrainingOutput>> GetAllAsync()
        {

            long count = await _programs.CountDocumentsAsync(_ => true);
            if (count == LIMIT_OF_PROGRAMS)
            {
                return GetRandomAsync(LIMIT_OF_PROGRAMS).Result;
            }
            else
            {
                return GetOrderAsync().Result;
            }
        }

        public async Task<TrainingOutput?> GetByIdAsync(String id)
        {
            string list_key = $"training_list";
            List<Training>? cached_trainings = JsonSerializer.Deserialize<List<Training>>(_cache.GetString(list_key));
            foreach (var element in cached_trainings) { 
                if (id.Equals(element.Id))
                {
                    return MapToOutput(element);
                }
            }
            return null;
        }

        public async Task<Training?> CreateAsync(TrainingInput program)
        {
            var coach = await _coaches.Find(element => element.Id.Equals(program.IdCoach)).FirstOrDefaultAsync();
            if (coach == null)
            {
                return null;
            }
            Stopwatch sw = Stopwatch.StartNew();
            var result = _programs.InsertOneAsync(MapToModel(program));
            await result;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return MapToModel(program);
        }

        public async Task<ReplaceOneResult?> UpdateAsync(String id, TrainingInput program)
        {
            var coach = await _coaches.Find(element => element.Id.Equals(program.IdCoach)).FirstOrDefaultAsync();
            if (coach == null)
            {
                return null;
            }
            var currentProgram = MapToModel(program);
            currentProgram.Id = id;
            Stopwatch sw = Stopwatch.StartNew();
            var result = _programs.ReplaceOneAsync(element => element.Id.Equals(id), currentProgram);
            await result;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return result.Result;
        }

        public async Task<DeleteResult> DeleteAsync(String id)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var task = _programs.DeleteOneAsync(element => element.Id.Equals(id));
            await task;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return task.Result;
        }

        public async Task<DeleteResult> DeleteAllAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var filter = Builders<Training>.Filter.Empty;
            var task = _programs.DeleteManyAsync(filter);
            await task;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return task.Result;
        }

        private static TrainingOutput MapToOutput(Training program)
        {
            return new TrainingOutput
            {
               Id = program.Id,
               Specializaion = program.Specializaion,
               Title = program.Title,
               Level = program.Level,
               IdCoach = program.IdCoach,
               Price = program.Price,
               Date = program.Date
            };
        }

        private static Training MapToModel(TrainingInput program)
        {
            return new Training
            {
                Specializaion = program.Specializaion,
                Title = program.Title,
                Level = program.Level,
                IdCoach = program.IdCoach,
                Price = program.Price,
                Date = program.Date
            };
        }
    }
}
