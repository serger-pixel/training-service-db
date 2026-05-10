using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace API_sprot_training_program.Models
{
    public class CoachInput
    {
        [Required]
        [MaxLength(Validation.MAX_LEN_STRING)]
        [MinLength(Validation.MIN_LEN_STRING)]
        public string Name { get; set; }

        [Required]
        [MaxLength(Validation.MAX_LEN_STRING)]
        [MinLength(Validation.MIN_LEN_STRING)]
        public string MiddleName { get; set; }

        [Required]
        [MaxLength(Validation.MAX_LEN_STRING)]
        [MinLength(Validation.MIN_LEN_STRING)]
        public string SecondName { get; set; }


        [EnumDataType(typeof(Education))]
        public Education MainEducation { get; set; }

        [EnumDataType(typeof(Education))]
        public Education SubEducation { get; set; }

        public List<TrainingType> Specializations { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime TimeConfirm { get; set; }
    }
}
