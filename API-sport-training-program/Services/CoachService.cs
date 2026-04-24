using API_sprot_training_program.Metrics;
using API_sprot_training_program.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace API_sprot_training_program.Services
{
    public class CoachService
    {
        private readonly IMongoCollection<Coach> _coaches;

        private readonly DataBaseRequestTime _data_base_metric;

        private const int LIMIT_OF_COACHES = 1000;

        public CoachService(IOptions<DataBaseSettings> settings, IMeterFactory meterFactory)
        {

            var mongoClient = new MongoClient(
            settings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);

            _coaches = mongoDatabase.GetCollection<Coach>(
                settings.Value.CollectionName);

            Type type = typeof(Coach);

            _data_base_metric = new DataBaseRequestTime(meterFactory);
        }


        public async Task<List<CoachOutput>> GetByFilter(String nameProperty, String value)
        {
            var property = typeof(CoachOutput).GetProperty(nameProperty);

            if (property == null) return new List<CoachOutput>();

            var targetType = property.PropertyType;
            var convertedValue = Convert.ChangeType(value, targetType);
            var filter = Builders<Coach>.Filter.Eq(nameProperty, convertedValue);
            var coachList = _coaches.Find(filter).ToListAsync();
            return coachList.Result.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<CoachOutput>> GetRandomAsync(int count)
        {
            var pipeline = new EmptyPipelineDefinition<Coach>()
                .Sample(count);
            Stopwatch sw = Stopwatch.StartNew();
            var coachList = _coaches.Aggregate(pipeline).ToListAsync();
            await coachList;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return coachList.Result.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<CoachOutput>> GetOrderAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var coachList = _coaches.Find(_ => true).ToListAsync();
            await coachList;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return coachList.Result.Select(
                element => MapToOutput(element)
                )
                .ToList();
        }

        public async Task<List<CoachOutput>> GetAllAsync()
        {

            long count = await _coaches.CountDocumentsAsync(_ => true);
            if (count == LIMIT_OF_COACHES)
            {
                return GetRandomAsync(LIMIT_OF_COACHES).Result;
            }
            else
            {
                return GetOrderAsync().Result;
            }
        }

        public async Task<CoachOutput?> GetByIdAsync(String id)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var element = _coaches.Find(element => element.Id.Equals(id)).FirstOrDefaultAsync();
            await element;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            if (element.Result == null)
            {
                return null;
            }
            return MapToOutput(element.Result);
        }

        public async Task CreateAsync(CoachInput coach)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var result = _coaches.InsertOneAsync(MapToModel(coach));
            await result;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
        }

        public async Task<ReplaceOneResult> UpdateAsync(String id, CoachInput coach)
        {
            var currentCoach = MapToModel(coach);
            currentCoach.Id = id;
            Stopwatch sw = Stopwatch.StartNew();
            var result = _coaches.ReplaceOneAsync(element => element.Id.Equals(id), currentCoach);
            await result;
            sw.Stop();
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return result.Result;
        }

        public async Task<DeleteResult> DeleteAsync(String id)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var task = _coaches.DeleteOneAsync(element => element.Id.Equals(id));
            await task;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return task.Result;
        }

        public async Task<DeleteResult> DeleteAllAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var filter = Builders<Coach>.Filter.Empty;
            var task = _coaches.DeleteManyAsync(filter);
            await task;
            _data_base_metric.add_value(sw.Elapsed.TotalMilliseconds);
            return task.Result;
        }

        private static CoachOutput MapToOutput(Coach coach)
        {
            return new CoachOutput
            {
                Id = coach.Id,
                Name = coach.Name,
                MiddleName = coach.MiddleName,
                SecondName = coach.SecondName,
                MainEducation = coach.MainEducation,
                SubEducation = coach.SubEducation,
                Specializations = new List<TrainingType>(coach.Specializations)
            };
        }

        private static Coach MapToModel(CoachInput coach)
        {
            return new Coach
            {
                Name = coach.Name,
                MiddleName = coach.MiddleName,
                SecondName = coach.SecondName,
                MainEducation = coach.MainEducation,
                SubEducation = coach.SubEducation,
                Specializations = new List<TrainingType>(coach.Specializations)
            };
        }
    }
}
