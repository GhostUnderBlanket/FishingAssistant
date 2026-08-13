using System.Globalization;
using Newtonsoft.Json;

namespace FishingAssistant.Configuration;

internal sealed class SafeStringEnumConverter<TEnum> : JsonConverter
    where TEnum : struct, Enum
{
    private static readonly TEnum InvalidValue = (TEnum)Enum.ToObject(typeof(TEnum), int.MaxValue);

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TEnum);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String
            && Enum.TryParse(reader.Value?.ToString(), ignoreCase: true, out TEnum parsed))
        {
            return parsed;
        }

        if (reader.TokenType == JsonToken.Integer)
        {
            try
            {
                long value = Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture);
                return Enum.ToObject(typeof(TEnum), value);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
                return InvalidValue;
            }
        }

        return InvalidValue;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }
}
