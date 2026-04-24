using API_sprot_training_program.Metrics;
using API_sprot_training_program.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace API_sprot_training_program.Services
{
    public class TrainingService
    {
        private readonly IMongoCollection<Training> _programs;

        private readonly DataBaseRequestTime _data_base_metric;

        private const int LIMIT_OF_PROGRAMS = 1000;

        public TrainingService(IOptions<DataBaseSettings> settings, IMeterFactory meterFactory)
        {

            var mongoClient = new MongoClient(
            settings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);

            _programs = mongoDatabase.GetCollection<Training>(
                settings.Value.CollectionName);

            Type type = typeof(Training);

            _data_base_metric = new DataBaseRequestTime(meterFactory);
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
            Stopwatch sw = Stopwatch.StartNew();
            var programsList = _programs.Find(_ => true).ToListAsync();
            await programsList;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return programsList.Result.Select(
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
            Stopwatch sw = Stopwatch.StartNew();
            var element = _programs.Find(element => element.Id.Equals(id)).FirstOrDefaultAsync();
            await element;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            if (element.Result == null)
            {
                return null;
            }
            return MapToOutput(element.Result);
        }

        public async Task CreateAsync(TrainingInput program)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var result = _programs.InsertOneAsync(MapToModel(program));
            await result;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
        }

        public async Task<ReplaceOneResult> UpdateAsync(String id, TrainingInput program)
        {
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
