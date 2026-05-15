using training_service_db.Metrics;
using training_service_db.Models;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace training_service_db.Services
{
    public class TrainingService
    {
        private readonly IMongoCollection<Training> _programs;
        private readonly IMongoCollection<Coach> _coaches;
        private readonly IDistributedCache _cache;
        private readonly DataBaseRequestTime _dataBaseMetric;
        private const int LimitOfPrograms = 1000;
        private const string CacheKey = "training_list";

        public TrainingService(
            IMongoClient mongoClient,
            IDataBaseSettings settings,
            IMeterFactory meterFactory,
            IDistributedCache cache)
        {
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _programs = mongoDatabase.GetCollection<Training>(settings.CollectionNameTraining);
            _coaches = mongoDatabase.GetCollection<Coach>(settings.CollectionNameCoach);
            _dataBaseMetric = new DataBaseRequestTime(meterFactory);
            _cache = cache;

            Task.Run(() => UpdateCache(CancellationToken.None));

            var dbPrograms = _programs.Find(_ => true).ToList();
            _cache.SetString(CacheKey, JsonSerializer.Serialize(dbPrograms));
        }

        public async Task<List<TrainingOutput>?> GetByFilter(TrainingsSchemaFilter properties)
        {
            var allTrainings = GetAllFromCache();

            if (allTrainings == null)
            {
                return null;
            }

            IEnumerable<Training> query = allTrainings;

            if (properties.IdCoach != null)
            {
                query = query.Where(t => t.IdCoach == properties.IdCoach);
            }

            if (properties.Level != null)
            {
                query = query.Where(t => t.Level == properties.Level);
            }

            if (properties.Price != null)
            {
                query = query.Where(t => t.Price == properties.Price);
            }

            var result = query.ToList();

            if (!result.Any())
            {
                return null;
            }

            return result.Select(MapToOutput).ToList();
        }

        public async Task<List<TrainingOutput>?> GetRandomAsync(int count)
        {
            var random = new Random();
            var cachedTrainings = GetAllFromCache();

            return cachedTrainings?
                .OrderBy(x => random.Next())
                .Take(count)
                .Select(MapToOutput)
                .ToList();
        }

        public async Task<List<TrainingOutput>?> GetOrderAsync()
        {
            var cachedTrainings = GetAllFromCache();
            return cachedTrainings?.Select(MapToOutput).ToList();
        }

        public async Task<List<TrainingOutput>?> GetAllAsync()
        {
            long count = await _programs.CountDocumentsAsync(_ => true);
            if (count == LimitOfPrograms)
            {
                return GetRandomAsync(LimitOfPrograms).Result;
            }
            else
            {
                return GetOrderAsync().Result;
            }
        }

        public async Task<TrainingOutput?> GetByIdAsync(string id)
        {
            return MapToOutput(GetByIdFromCache(id));
        }

        public async Task<Training?> CreateAsync(TrainingInput program)
        {
            var coach = await _coaches.Find(element => element.Id.Equals(program.IdCoach)).FirstOrDefaultAsync();
            if (coach == null)
            {
                return null;
            }

            var model = MapToModel(program);
            Stopwatch sw = Stopwatch.StartNew();
            await _programs.InsertOneAsync(model);
            sw.Stop();

            return model;
        }

        public async Task<ReplaceOneResult?> UpdateAsync(string id, TrainingInput program)
        {
            var coach = await _coaches.Find(element => element.Id.Equals(program.IdCoach)).FirstOrDefaultAsync();
            if (coach == null)
            {
                return null;
            }

            var currentProgram = MapToModel(program);
            currentProgram.Id = id;

            var result = await _programs.ReplaceOneAsync(element => element.Id.Equals(id), currentProgram);

            return result;
        }

        public async Task<DeleteResult> DeleteAsync(string id)
        {
            var result = await _programs.DeleteOneAsync(element => element.Id.Equals(id));
            return result;
        }

        public async Task<DeleteResult> DeleteAllAsync()
        {
            var filter = Builders<Training>.Filter.Empty;
            var result = await _programs.DeleteManyAsync(filter);
            return result;
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

        private async Task UpdateCache(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var dbTrainings = _programs.Find(_ => true).ToList();
                _cache.SetString(CacheKey, JsonSerializer.Serialize(dbTrainings));
            }
        }

        private List<Training>? GetAllFromCache()
        {
            var json = _cache.GetString(CacheKey);
            if (json == null) return null;
            return JsonSerializer.Deserialize<List<Training>>(json);
        }

        private Training? GetByIdFromCache(string id)
        {
            var dbTrainings = GetAllFromCache();
            if (dbTrainings == null) return null;

            foreach (var element in dbTrainings)
            {
                if (id.Equals(element.Id))
                {
                    return element;
                }
            }
            return null;
        }
    }
}