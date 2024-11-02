using System;

namespace SphereKit
{
    public class PlayerDataOperation
    {
        public readonly PlayerDataOperationType OperationType;
        public readonly object Value;

        private PlayerDataOperation(PlayerDataOperationType operationType, object value)
        {
            OperationType = operationType;
            Value = value;
        }

        public static PlayerDataOperation Set(object value)
        {
            return new PlayerDataOperation(PlayerDataOperationType.Set, value);
        }

        static void CheckNumberValue(object value)
        {
            if (value is not long && value is not int && value is not float && value is not double)
            {
                throw new ArgumentException("Value must be a number (long/int/float/double).");
            }
        }

        public static PlayerDataOperation Inc(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Inc, value);
        }

        public static PlayerDataOperation Dec(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Dec, value);
        }

        public static PlayerDataOperation Min(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Min, value);
        }

        public static PlayerDataOperation Max(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Max, value);
        }

        public static PlayerDataOperation Mul(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Mul, value);
        }

        public static PlayerDataOperation Div(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Div, value);
        }

        public static PlayerDataOperation Unset()
        {
            return new PlayerDataOperation(PlayerDataOperationType.Unset, null);
        }
    }

    public enum PlayerDataOperationType
    {
        Set,
        Inc,
        Dec,
        Min,
        Max,
        Mul,
        Div,
        Unset
    }
}