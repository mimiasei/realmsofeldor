using System;
using Newtonsoft.Json;

namespace RealmsOfEldor.Core.Serialization
{
    /// <summary>
    /// Custom JSON converter for Position struct.
    /// Serializes as compact object: {"x":5,"y":10}
    /// </summary>
    public class PositionJsonConverter : JsonConverter<Position>
    {
        public override void WriteJson(JsonWriter writer, Position value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Position ReadJson(JsonReader reader, Type objectType, Position existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new Position(0, 0);

            int x = 0, y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value.ToString().ToLower();
                    reader.Read(); // Move to value

                    if (propertyName == "x")
                        x = Convert.ToInt32(reader.Value);
                    else if (propertyName == "y")
                        y = Convert.ToInt32(reader.Value);
                }
            }

            return new Position(x, y);
        }
    }
}
