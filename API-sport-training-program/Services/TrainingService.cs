using API_sprot_training_program.Metrics;
using API_sprot_training_program.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
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

        public TrainingService(IOptions<DataBaseSettings> settings, IMeterFactory meterFactory, IDistributedCache cache)
        {

            var mongoClient = new MongoClient(
            settings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);

            _programs = mongoDatabase.GetCollection<Training>(
                settings.Value.CollectionNameTraining);

            _coaches = mongoDatabase.GetCollection<Coach>(
                settings.Value.CollectionNameCoach);

            Type type = typeof(Training);

            _data_base_metric = new DataBaseRequestTime(meterFactory);

            _cache = cache;
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
            var pipeline = new EmptyPipelineDefinition<Training>()        
                .Sample(count);
            Stopwatch sw = Stopwatch.StartNew();
            var programsList = _programs.Aggregate(pipeline).ToListAsync();
            await programsList;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return programsList.Result.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<TrainingOutput>> GetOrderAsync()
        {
            string list_key = $"training_ids";

            List<string> cache_ids =  JsonSerializer.Deserialize<List<string>>(_cache.GetString(list_key));
            List<string> missed_id = new List<string>();

            List<TrainingOutput> cached_trainings = new List<TrainingOutput>();

            foreach(var id in cache_ids){
                string id_key = $"training_{id}";
                Training program = JsonSerializer.Deserialize<Training>(_cache.GetString(id_key));
                if (program == null) { 
                    missed_id.Add(id_key);
                }
                else { 
                    cached_trainings.Add(MapToOutput(program));
                }
            }

            Stopwatch sw = Stopwatch.StartNew();
            var db_trainings = _programs.Find(p => missed_id.Contains(p.Id)).ToList<Training>();
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            List<TrainingOutput> result = new List<TrainingOutput>();
            result.AddRange(cached_trainings);
            result.AddRange(db_trainings.Select(
                element => MapToOutput(element)
                )
                .ToList());


            return result;
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
            string key = $"training_{id}";
            var cache_element = await _cache.GetAsync(key);

            if (cache_element == null)
            {
                Stopwatch sw = Stopwatch.StartNew();
                var element = _programs.Find(element => element.Id.Equals(id)).FirstOrDefaultAsync();
                await element;
                if (element == null)
                {
                    return null;
                }
                else
                {
                    if (element.Result.Specializaion == TrainingType.Cardio ||
                        element.Result.Specializaion == TrainingType.Power)
                    {
                        _cache.SetString(key, JsonSerializer.Serialize(element), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                        });
                    }
                }
                _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
                return MapToOutput(element.Result);
            }
            return JsonSerializer.Deserialize<TrainingOutput>(cache_element);
        }

        public async Task<Training?> CreateAsync(TrainingInput program)
        {
            var coach = await _coaches.Find(element => element.Id.Equals(program.IdCoach)).FirstOrDefaultAsync();
            if (coach == null)
            {
                return null;
            }
            Stopwatch sw = Stopwatch.StartNew();
            Training new_training = MapToModel(program);
            var result = _programs.InsertOneAsync(new_training);
            await result;

            string key = "training_ids";
            var cached_list = _cache.GetString(key);
            List<string> cached_ids;

            if (cached_list != null)
            {
                cached_ids = JsonSerializer.Deserialize<List<string>>(cached_list);
                cached_ids.Add(new_training.Id);
                _cache.SetString(key, JsonSerializer.Serialize(cached_ids));
            }
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
               Price = program.Price
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
                Price = program.Price
            };
        }
    }
}
