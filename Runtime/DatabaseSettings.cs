#nullable enable
namespace SphereKit
{
    public class DatabaseSettings
    {
        public string? DatabaseId { get; }

        public DatabaseSettings(string? databaseId)
        {
            DatabaseId = databaseId;
        }
    }
}