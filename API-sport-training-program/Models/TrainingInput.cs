using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace API_sprot_training_program.Models
{

    public class TrainingInput
    {
        [Required]
        [EnumDataType(typeof(TrainingType))]
        public TrainingType Specializaion { get; set; }

        [Required]
        [MaxLength(Validation.MAX_LEN_STRING)]
        [MinLength(Validation.MIN_LEN_STRING)]
        public String Title { get; set; }

        [Required]
        [EnumDataType(typeof(TrainingType))]
        public Difficulty Level { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdCoach { get; set; }

        [Required]
        [Range(typeof(Decimal), Validation.MIN_PRICE, Validation.MAX_PRICE)]
        public Decimal Price { get; set; }

        public System.DateTime Date { get; set; }

    }
}
