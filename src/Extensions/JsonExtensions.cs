using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SwitchBotInfluxDbExporter.Extensions;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions DefaultSerializerSettings = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    public static string JsonFormatting(this string o)
    {
        var jsonDocument = JsonDocument.Parse(o);

        return JsonSerializer.Serialize(jsonDocument, DefaultSerializerSettings);
    }
}
