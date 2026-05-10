using MongoDB.Bson.Serialization.Attributes;

namespace API_sprot_training_program.Models
{
    public enum Education
    {
        HigherEducation,
        SecondaryVocationalEducation
    }

    public class Coach
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public String Id { get; set; }
        public string Name { get; set; }
        public string MiddleName {  get; set; }
        public string SecondName { get; set; }
        public Education MainEducation { get; set; }
        public Education SubEducation { get; set; }
        public List<TrainingType> Specializations { get; set; }
        public string UserId { get; set; }
        public string TimeConfirm { get; set; }
    }
}
