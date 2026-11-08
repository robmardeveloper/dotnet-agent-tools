using System.Xml.Linq;

namespace DotnetAgentTools.Testing;

internal enum TestOutcome
{
    Passed,
    Failed,
    Skipped,
}

/// <summary>One test result as written in the TRX file.</summary>
internal sealed record TestResult(string Name, TestOutcome Outcome, string? Message, string? StackTrace);

/// <summary>Reads the test results from a TRX file (the Visual Studio test results XML), with no extra library.</summary>
internal static class TrxResults
{
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static IReadOnlyList<TestResult> Read(string trxPath)
    {
        var document = XDocument.Load(trxPath);
        return document.Descendants(Trx + "UnitTestResult")
            .Select(result =>
            {
                var error = result.Element(Trx + "Output")?.Element(Trx + "ErrorInfo");
                return new TestResult(
                    (string?)result.Attribute("testName") ?? "(unnamed test)",
                    ToOutcome((string?)result.Attribute("outcome")),
                    error?.Element(Trx + "Message")?.Value,
                    error?.Element(Trx + "StackTrace")?.Value);
            })
            .ToList();
    }

    /// <summary>TRX has many outcomes; for the agent a test passed, was skipped, or needs attention.</summary>
    private static TestOutcome ToOutcome(string? outcome) => outcome switch
    {
        "Passed" => TestOutcome.Passed,
        "NotExecuted" or "Inconclusive" => TestOutcome.Skipped,
        _ => TestOutcome.Failed,
    };
}
