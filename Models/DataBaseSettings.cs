namespace training_service_db.Models
{

    public interface IDataBaseSettings {
        public string DatabaseName { get; set; }

        public string CollectionNameCoach { get; set; }

        public string CollectionNameTraining { get; set; }
    }

    public class TraningsDataBaseSettings : IDataBaseSettings
    {
        public TraningsDataBaseSettings(string collectionNameCoach, string collectionNameTraining, string databaseName)
        {
            DatabaseName = databaseName;
            CollectionNameCoach = collectionNameCoach;
            CollectionNameTraining = collectionNameTraining;

        }
        public string DatabaseName { get; set; } = null!;

        public string CollectionNameCoach { get; set; } = null!;

        public string CollectionNameTraining { get; set; } = null!;
    }
}
