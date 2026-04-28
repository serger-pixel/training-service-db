using MongoDB.Bson.Serialization.Attributes;

namespace API_sprot_training_program.Models
{
    public enum TrainingType
    {
        Cardio,
        Power,
        Functional,
        Dance,
        Yoga,
        Pilates
    }

    public enum Difficulty
    {
        Low,
        Middle,
        High
    }

    public class Training
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public TrainingType Specializaion { get; set; }

        public String Title { get; set; }

        public Difficulty Level { get; set; }

        public string IdCoach {  get; set; }

        public Decimal Price {  get; set; }

        public System.DateTime Date { get; set; }

    }
}
