using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mesasitec.Api.Serializacion;

// Serializa TODOS los DateTime en ISO-8601 UTC con sufijo Z (§6).
// SQLite pierde el DateTimeKind al leer, así que forzamos el formato aquí,
// en la salida, sin importar cómo venga marcada la fecha.
public class DateTimeUtcConverter : JsonConverter<DateTime>
{
    private const string Formato = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Tratamos la fecha como UTC y la escribimos con Z, pase lo que pase.
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString(Formato, CultureInfo.InvariantCulture));
    }
}
// Igual, pero para DateTime? (fechas opcionales como fechaResolucion).
public class DateTimeUtcNullableConverter : JsonConverter<DateTime?>
{
    private const string Formato = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        var utc = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString(Formato, CultureInfo.InvariantCulture));
    }
}