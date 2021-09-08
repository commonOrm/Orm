using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //Debug.Assert(typeToConvert == typeof(DateTime));
        return DateTime.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        //时间是yyyy-MM-dd HH:mm:ss这种格式会造成IOS端报错
        //writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        
        writer.WriteStringValue(value.ToString("yyyy/MM/dd HH:mm:ss"));
    }
}