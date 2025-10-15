using System;
using Newtonsoft.Json;

namespace RealmsOfEldor.Core.Serialization
{
    /// <summary>
    /// Custom JSON converter for ResourceSet struct.
    /// Serializes as object with named properties for readability:
    /// {"gold":1000,"wood":20,"ore":15,"crystal":5,"gems":5,"sulfur":5,"mercury":5}
    /// </summary>
    public class ResourceSetJsonConverter : JsonConverter<ResourceSet>
    {
        public override void WriteJson(JsonWriter writer, ResourceSet value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("gold");
            writer.WriteValue(value.Gold);
            writer.WritePropertyName("wood");
            writer.WriteValue(value.Wood);
            writer.WritePropertyName("ore");
            writer.WriteValue(value.Ore);
            writer.WritePropertyName("crystal");
            writer.WriteValue(value.Crystal);
            writer.WritePropertyName("gems");
            writer.WriteValue(value.Gems);
            writer.WritePropertyName("sulfur");
            writer.WriteValue(value.Sulfur);
            writer.WritePropertyName("mercury");
            writer.WriteValue(value.Mercury);
            writer.WriteEndObject();
        }

        public override ResourceSet ReadJson(JsonReader reader, Type objectType, ResourceSet existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new ResourceSet();

            int gold = 0, wood = 0, ore = 0, crystal = 0, gems = 0, sulfur = 0, mercury = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value.ToString().ToLower();
                    reader.Read(); // Move to value

                    switch (propertyName)
                    {
                        case "gold":
                            gold = Convert.ToInt32(reader.Value);
                            break;
                        case "wood":
                            wood = Convert.ToInt32(reader.Value);
                            break;
                        case "ore":
                            ore = Convert.ToInt32(reader.Value);
                            break;
                        case "crystal":
                            crystal = Convert.ToInt32(reader.Value);
                            break;
                        case "gems":
                            gems = Convert.ToInt32(reader.Value);
                            break;
                        case "sulfur":
                            sulfur = Convert.ToInt32(reader.Value);
                            break;
                        case "mercury":
                            mercury = Convert.ToInt32(reader.Value);
                            break;
                    }
                }
            }

            return new ResourceSet(gold, wood, ore, crystal, gems, sulfur, mercury);
        }
    }
}
