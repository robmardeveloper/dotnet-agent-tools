using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetAgentTools;

/// <summary>
/// How every tool writes its answer: compact JSON, camelCase, no empty fields, and quotes and accents left as they are
/// instead of escaped (an escaped character costs the agent several tokens and is harder to read).
/// </summary>
internal static class AgentJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
