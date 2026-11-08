using System.Text.Json;

namespace DotnetAgentTools.Testing;

/// <summary>
/// Tells whether dotnet test runs on Microsoft.Testing.Platform (MTP) in a folder. Since .NET 10 that is chosen in
/// global.json, which dotnet looks for from the working folder upwards; without it, dotnet test uses VSTest.
/// </summary>
internal static class TestPlatform
{
    private const string GlobalJson = "global.json";
    private const string Mtp = "Microsoft.Testing.Platform";

    private static readonly JsonDocumentOptions Lenient = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static bool UsesMtp(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            var file = Path.Combine(current.FullName, GlobalJson);
            if (File.Exists(file))
            {
                return ReadsMtp(file);
            }
        }

        return false;
    }

    private static bool ReadsMtp(string file)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file), Lenient);
            return document.RootElement.TryGetProperty("test", out var test)
                && test.TryGetProperty("runner", out var runner)
                && runner.ValueKind == JsonValueKind.String
                && string.Equals(runner.GetString(), Mtp, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
