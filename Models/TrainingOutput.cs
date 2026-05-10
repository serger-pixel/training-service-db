using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace training_service_db.Models
{
    public class TrainingOutput
    {
        public string Id { get; set; }

        public TrainingType Specializaion { get; set; }

        public String Title { get; set; }

        public Difficulty Level { get; set; }

        public string IdCoach { get; set; }

        public Decimal Price { get; set; }

        public System.DateTime Date { get; set; }

    }
}
