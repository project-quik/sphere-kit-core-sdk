#nullable enable
namespace SphereKit
{
    /// <summary>
    /// The settings for the database module.
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// The ID of the database.
        /// </summary>
        public string? DatabaseId { get; }

        public DatabaseSettings(string? databaseId)
        {
            DatabaseId = databaseId;
        }
    }
}