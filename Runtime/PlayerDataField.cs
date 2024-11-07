#nullable enable
namespace SphereKit
{
    public class PlayerDataField
    {
        public static PlayerDataField Level { get { return new PlayerDataField("level"); } }
        public static PlayerDataField Score { get { return new PlayerDataField("score"); } }
        public static PlayerDataField Metadata(string key)
        {
            return new PlayerDataField($"metadata.{key}");
        }

        internal readonly string key;

        private PlayerDataField(string key)
        {
            this.key = key;
        }
        public override bool Equals(object obj)
        {
            if (obj is PlayerDataField other)
            {
                return key == other.key;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }
    }
}
