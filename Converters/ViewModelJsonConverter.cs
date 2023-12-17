using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace KinoshitaProductions.Emvvm.Converters
{
    public class ViewModelJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ViewModelEntry).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            try
            {
                var jObject = JToken.ReadFrom(reader);
                var kind = jObject.Value<string>("k");
                if (kind == null) return null;
                var value = (ObservableViewModel?)jObject["v"]?.ToObject(ViewModelManager.GetMappingFor(kind).ViewModelType);
                if (value == null) return null;
                return new ViewModelEntry
                {
                    Kind = kind,
                    Value = value
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read viewModelEntry");
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typedValue = (ViewModelEntry?)value;
            if (typedValue == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartObject();
            writer.WritePropertyName("k");
            writer.WriteValue(typedValue.Kind);
            writer.WritePropertyName("v");
            var jo = JObject.FromObject(typedValue.Value); jo.WriteTo(writer);
            writer.WriteEndObject();
        }
    }
}