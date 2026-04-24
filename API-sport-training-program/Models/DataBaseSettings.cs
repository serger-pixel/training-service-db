namespace API_sprot_training_program.Models
{
    public class DataBaseSettings
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string CollectionNameCoach { get; set; } = null!;

        public string CollectionNameTraining { get; set; } = null!;
    }
}
