using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace training_service_db.Models
{
    public class TrainingsSchemaFilter
    {
        [EnumDataType(typeof(Difficulty), ErrorMessage = "Указан некорректный уровень сложности.")]
        public Difficulty Level { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string IdCoach {  get; set; }

        [Range(typeof(Decimal), Validation.MIN_PRICE, Validation.MAX_PRICE,
           ErrorMessage = $"Недопустимая цена. Допустимый диапазон: от {Validation.MIN_PRICE} до {Validation.MAX_PRICE}.")]
        public Decimal Price {  get; set; }
    }
}
    