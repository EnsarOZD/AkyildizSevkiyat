using System.Text.Json;
using System.Text.Json.Serialization;

namespace Akyildiz.Sevkiyat.WebApi.Infrastructure
{
    /// <summary>
    /// Ensures DateTime values with DateTimeKind.Unspecified (EF Core default from SQL Server)
    /// are serialized as UTC (appending Z suffix) so browsers in other timezones display them correctly.
    /// DateTimeKind.Local values are converted to UTC before serialization.
    /// </summary>
    public class DateTimeUtcJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc), // Unspecified — assume UTC (EF Core SQL Server default)
            };
            writer.WriteStringValue(utc);
        }
    }

    public class NullableDateTimeUtcJsonConverter : JsonConverter<DateTime?>
    {
        private static readonly DateTimeUtcJsonConverter _inner = new();

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : _inner.Read(ref reader, typeof(DateTime), options);

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue) writer.WriteNullValue();
            else _inner.Write(writer, value.Value, options);
        }
    }
}
