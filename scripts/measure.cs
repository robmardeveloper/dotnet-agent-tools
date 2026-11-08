#:package ModelContextProtocol
#:package Microsoft.ML.Tokenizers
#:package Microsoft.ML.Tokenizers.Data.O200kBase

// Measures what an agent reads with and without dotnet-agent-tools, on the sample solution of dotnet-agent-kit.
//
//   dotnet run scripts/measure.cs -- <path to dotnet-agent-kit/sample>
//
// It copies the sample to a temporary folder and breaks it in two ways:
//   build: a typo in the code, so the build fails with one compiler error;
//   test:  a booking rule changed from 7 to 10 days ahead, so two tests fail.
// For each one it keeps the console output of plain 'dotnet build' / 'dotnet test' (before) and the answer of the
// tool, received over MCP exactly as an agent receives it (after), and prints a table with lines, characters and
// tokens. Tokens are counted with the o200k tokenizer, on this machine; other models count a little differently.

using System.Diagnostics;
using System.Globalization;
using Microsoft.ML.Tokenizers;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

if (args.Length != 1 || !File.Exists(Path.Combine(args[0], "CourtBookings.sln")))
{
    Console.Error.WriteLine("Usage: dotnet run scripts/measure.cs -- <path to dotnet-agent-kit/sample>");
    return 1;
}

var repository = Path.GetFullPath(Path.Combine(
    AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "scripts", ".."));
var sample = Path.GetFullPath(args[0]);

Console.Error.WriteLine("Building the server...");
await RunAsync(repository, englishMessages: true, "build", Path.Combine("src", "DotnetAgentTools"), "-c", "Release", "-nologo", "-v:q");
var server = Path.Combine(repository, "src", "DotnetAgentTools", "bin", "Release", "net10.0", "DotnetAgentTools.dll");

var work = Directory.CreateTempSubdirectory("dotnet-agent-tools-measure-").FullName;
var solution = Path.Combine(work, "sample");
CopySample(sample, solution);

// Restore and build once, so both sides measure the usual case: a build after a small change.
Console.Error.WriteLine("Building the sample once...");
await RunAsync(solution, englishMessages: true, "build", "CourtBookings.sln");

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "dotnet-agent-tools",
    Command = "dotnet",
    Arguments = [server],
    EnvironmentVariables = new Dictionary<string, string?> { ["Workspace__Root"] = solution },
});
await using var client = await McpClient.CreateAsync(transport);
var tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");
var outputs = Directory.CreateDirectory(Path.Combine(work, "outputs")).FullName;
var rows = new List<string>();

await MeasureAsync(
    "build", "one compiler error",
    Path.Combine("src", "CourtBookings.Application", "Bookings", "BookingRepositoryExtensions.cs"),
    "return booking;", "return bookng;",
    () => RunAsync(solution, englishMessages: true, "build", "CourtBookings.sln"));

// dotnet test runs the user's tests, so it keeps the machine's language, as the tool does.
await MeasureAsync(
    "test", "two failing tests",
    Path.Combine("src", "CourtBookings.Domain", "Bookings", "Booking.cs"),
    "TimeSpan.FromDays(7);", "TimeSpan.FromDays(10);",
    () => RunAsync(solution, englishMessages: false, "test", "--solution", "CourtBookings.sln"));

Console.WriteLine("| Case | Output | Lines | Characters | Tokens (o200k) |");
Console.WriteLine("|---|---|---:|---:|---:|");
rows.ForEach(Console.WriteLine);
Console.Error.WriteLine();
Console.Error.WriteLine($"Raw outputs: {outputs}");
Console.Error.WriteLine("They hold local paths: look at them, but do not commit them.");
return 0;

async Task MeasureAsync(string tool, string label, string file, string original, string broken, Func<Task<string>> runPlain)
{
    var path = Path.Combine(solution, file);
    var code = await File.ReadAllTextAsync(path);
    if (!code.Contains(original, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"'{original}' not found in {file}: the sample has changed.");
    }

    await File.WriteAllTextAsync(path, code.Replace(original, broken, StringComparison.Ordinal));
    try
    {
        Console.Error.WriteLine($"Measuring {tool}: {label}...");
        var before = await runPlain();
        var result = await client.CallToolAsync(tool, new Dictionary<string, object?>());
        var after = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        await File.WriteAllTextAsync(Path.Combine(outputs, $"{tool}-before.txt"), before);
        await File.WriteAllTextAsync(Path.Combine(outputs, $"{tool}-after.json"), after);
        rows.Add(Row($"{tool}: {label}", $"dotnet {tool}", before));
        rows.Add(Row($"{tool}: {label}", $"`{tool}` tool", after));
    }
    finally
    {
        await File.WriteAllTextAsync(path, code);
    }
}

string Row(string scenario, string output, string text)
{
    var lines = text.TrimEnd().Split('\n').Length;
    return string.Create(
        CultureInfo.InvariantCulture, $"| {scenario} | {output} | {lines:N0} | {text.Length:N0} | {tokenizer.CountTokens(text):N0} |");
}

static async Task<string> RunAsync(string folder, bool englishMessages, params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet", arguments)
    {
        WorkingDirectory = folder,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    if (englishMessages)
    {
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        startInfo.Environment["VSLANG"] = "1033";
    }

    using var process = Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    return await output + await error;
}

static void CopySample(string source, string destination)
{
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.GetFiles(source))
    {
        File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
    }

    foreach (var folder in Directory.GetDirectories(source))
    {
        var name = Path.GetFileName(folder);
        if (name is not ("bin" or "obj" or "TestResults" or ".vs"))
        {
            CopySample(folder, Path.Combine(destination, name));
        }
    }
}
