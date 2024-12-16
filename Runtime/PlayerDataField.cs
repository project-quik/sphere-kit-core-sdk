#nullable enable
namespace SphereKit
{
    public class PlayerDataField
    {
        public static PlayerDataField Level => new("level");
        public static PlayerDataField Score => new("score");

        public static PlayerDataField Metadata(string key)
        {
            return new PlayerDataField($"metadata.{key}");
        }

        internal readonly string Key;

        private PlayerDataField(string key)
        {
            Key = key;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PlayerDataField other) return Key == other.Key;
            return false;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }
}