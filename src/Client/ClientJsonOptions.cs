using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectResourceManagement.Client;

internal static class ClientJsonOptions
{
    public static JsonSerializerOptions Instance { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
