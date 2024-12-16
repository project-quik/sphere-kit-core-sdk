using System;
using System.Collections.Generic;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// An update operation on a player data field.
    /// </summary>
    public class PlayerDataOperation
    {
        /// <summary>
        /// The type of operation.
        /// </summary>
        public readonly PlayerDataOperationType OperationType;

        /// <summary>
        /// The value to apply to the field during the operation.
        /// </summary>
        public readonly object? Value;

        private PlayerDataOperation(PlayerDataOperationType operationType, object? value)
        {
            OperationType = operationType;
            Value = value;
        }

        /// <summary>
        /// Sets the field to the specified value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns></returns>
        public static PlayerDataOperation Set(object value)
        {
            return new PlayerDataOperation(PlayerDataOperationType.Set, value);
        }

        /// <summary>
        /// Checks if a value is a number (long/int/float/double).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <exception cref="ArgumentException">The value is not a number.</exception>
        private static void CheckNumberValue(object value)
        {
            if (value is not long && value is not int && value is not float && value is not double)
                throw new ArgumentException("Value must be a number (long/int/float/double).");
        }

        /// <summary>
        /// Increments the field by the specified value.
        /// </summary>
        /// <param name="value">The value to increment by.</param>
        /// <returns></returns>
        public static PlayerDataOperation Inc(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Inc, value);
        }

        /// <summary>
        /// Decrements the field by the specified value.
        /// </summary>
        /// <param name="value">The value to decrement by.</param>
        /// <returns></returns>
        public static PlayerDataOperation Dec(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Dec, value);
        }

        /// <summary>
        /// Sets the field to the minimum of the current value and the specified value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns></returns>
        public static PlayerDataOperation Min(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Min, value);
        }

        /// <summary>
        /// Sets the field to the maximum of the current value and the specified value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns></returns>
        public static PlayerDataOperation Max(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Max, value);
        }

        /// <summary>
        /// Multiplies the field by the specified value.
        /// </summary>
        /// <param name="value">The value to multiply by.</param>
        /// <returns></returns>
        public static PlayerDataOperation Mul(object value)
        {
            CheckNumberValue(value);

            return new PlayerDataOperation(PlayerDataOperationType.Mul, value);
        }

        /// <summary>
        /// Divides the field by the specified value.
        /// </summary>
        /// <param name="value">The value to divide by.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Cannot divide by zero.</exception>
        public static PlayerDataOperation Div(object value)
        {
            CheckNumberValue(value);
            if ((int)value == 0)
                throw new ArgumentException("Cannot divide by zero.");

            return new PlayerDataOperation(PlayerDataOperationType.Div, value);
        }

        /// <summary>
        /// Unsets the field (removes the field and its data).
        /// </summary>
        /// <returns></returns>
        public static PlayerDataOperation Unset()
        {
            return new PlayerDataOperation(PlayerDataOperationType.Unset, null);
        }

        /// <summary>
        /// Gets the string representation of the operation type.
        /// </summary>
        /// <param name="operationType">The operation type.</param>
        /// <returns>The string representation.</returns>
        private static string GetStringOperationType(PlayerDataOperationType operationType)
        {
            return operationType switch
            {
                PlayerDataOperationType.Set => "$set",
                PlayerDataOperationType.Inc => "$inc",
                PlayerDataOperationType.Dec => "$dec",
                PlayerDataOperationType.Min => "$min",
                PlayerDataOperationType.Max => "$max",
                PlayerDataOperationType.Mul => "$mul",
                PlayerDataOperationType.Div => "$div",
                PlayerDataOperationType.Unset => "$unset",
                _ => ""
            };
        }

        /// <summary>
        /// This method converts a dictionary of update operations into a dictionary of request data that can be sent to the server.
        /// </summary>
        /// <param name="update">The update specification, with field as key and operation as value.</param>
        /// <returns>The request data.</returns>
        internal static Dictionary<string, object> ConvertUpdateToRequestData(
            Dictionary<PlayerDataField, PlayerDataOperation> update)
        {
            var updateRequestData = new Dictionary<string, object>();
            foreach (var keyValuePair in update)
            {
                var fieldKey = keyValuePair.Key.Key;
                var operationKey = keyValuePair.Value.OperationType;
                var operationValue = keyValuePair.Value.Value;
                var operationKeyStr = GetStringOperationType(operationKey);

                // Unset operation fields will be sent as a list of field keys.
                if (operationKey != PlayerDataOperationType.Unset)
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new Dictionary<string, object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }

                    var operationDataDict = (Dictionary<string, object>)operationData;
                    operationDataDict[fieldKey] = operationValue!;
                }
                // Other operation fields will be sent as a dictionary of field and values.
                else
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new List<object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }

                    var operationDataList = (List<object>)operationData;
                    operationDataList.Add(fieldKey);
                }
            }

            return updateRequestData;
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