using API_sprot_training_program.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

public class TrainingInput
{
    [Required(ErrorMessage = "Специализация обязательна для заполнения.")]
    [EnumDataType(typeof(TrainingType), ErrorMessage = "Указано некорректное значение специализации.")]
    public TrainingType Specializaion { get; set; }

    [Required(ErrorMessage = "Название тренировки обязательно.")]
    [MinLength(Validation.MIN_LEN_STRING, ErrorMessage = "Название слишком короткое. Минимум символов: 5.")]
    [MaxLength(Validation.MAX_LEN_STRING, ErrorMessage = "Название слишком длинное. Максимум символов: 255.")]
    public String Title { get; set; }

    [Required(ErrorMessage = "Уровень сложности обязателен.")]
    [EnumDataType(typeof(Difficulty), ErrorMessage = "Указан некорректный уровень сложности.")]
    public Difficulty Level { get; set; }

    
    [Required(ErrorMessage = "ID тренера обязателен.")]
    [BsonRepresentation(BsonType.ObjectId)]
   
    public string IdCoach { get; set; }

    
    [Required(ErrorMessage = "Цена обязательна.")]
    [Range(typeof(Decimal), Validation.MIN_PRICE, Validation.MAX_PRICE,
           ErrorMessage = $"Недопустимая цена. Допустимый диапазон: от {Validation.MIN_PRICE} до {Validation.MAX_PRICE}.")]
    public Decimal Price { get; set; }


    [Required(ErrorMessage = "Дата проведения обязательна.")]
    [BsonRepresentation(BsonType.DateTime)]
    public System.DateTime Date { get; set; }
}