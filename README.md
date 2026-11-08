# dotnet-agent-tools

A small MCP server that gives AI coding agents the short, exact result of `dotnet build` and `dotnet test`,
instead of the full console output.

An agent that runs `dotnet test` reads every line the command prints: progress, logs, stack traces, summaries.
Most of it is noise, and all of it costs tokens and attention. This server runs the same commands and returns only
what the agent needs to fix the code: each error or failed test, with file, line and message.

It has two tools:

| Tool | What it does | What it returns |
|---|---|---|
| `build` | Runs `dotnet build` and reads the binary log | Each error with code, file, line and column; warnings counted by code |
| `test` | Builds first, then runs `dotnet test` and reads the TRX report | Counts, and each failed test with message, file and line. If the build fails, the build errors |

## What the agent gets

A build with one compiler error:

```json
{"ok":false,"errors":1,"warnings":1,"items":[{"sev":"error","code":"CS0103","file":"Program.cs","line":2,"col":19,"msg":"The name 'totl' does not exist in the current context","project":"CompileError"}],"warningsByCode":{"CS8602":1},"truncated":false,"durationMs":2139}
```

A test run with two failures:

```json
{"ok":false,"total":4,"passed":1,"failed":2,"skipped":1,"failures":[{"test":"FailingTests.PriceTests.Total_WithDiscount_IsLower","msg":"Assert.Equal() Failure: Values differ\nExpected: 4\nActual:   5","file":"PriceTests.cs","line":11},{"test":"FailingTests.PriceTests.Parse_Empty_Throws","msg":"System.FormatException : The price is empty.","file":"PriceTests.cs","line":25}],"truncated":false,"durationMs":2845}
```

Paths are relative to the workspace. Long messages are cut, and lists stop at a limit the agent can raise
(`maxItems`, `maxFailures`); `truncated` says when that happened.

## Before and after

Measured on the sample solution of [dotnet-agent-kit](https://github.com/robmardeveloper/dotnet-agent-kit),
broken on purpose in two ways:

| Case | Output | Lines | Characters | Tokens (o200k) |
|---|---|---:|---:|---:|
| build: one compiler error | dotnet build | 12 | 1,091 | 310 |
| build: one compiler error | `build` tool | 1 | 314 | 83 |
| test: two failing tests | dotnet test | 1,248 | 75,914 | 19,504 |
| test: two failing tests | `test` tool | 1 | 741 | 187 |

How to read it:

- **The build saving is small**, because `dotnet build` is already short when there is one error. It can grow with the
  number of projects and warnings.
- **The test saving is large mostly because of logs.** One of the failing tests is an integration test, and
  `dotnet test` prints the Entity Framework Core logs it wrote. That is common in real projects, but it is the reason
  for most of the 19,504 tokens, not the test runner itself.
- Measured on Windows with the console in Spanish. The language of the messages and the length of the paths change
  the numbers a little. Tokens are counted with the `o200k_base` tokenizer; other models count a little differently.

In one manual try with Claude Code on the broken sample, the agent chose the `test` tool by itself, without being told
to, found the changed rule from the two failures, and called the tool again to check its fix. One try, not a
measurement.

To reproduce the table, with a copy of dotnet-agent-kit next to this repository:

```
dotnet run scripts/measure.cs -- ../dotnet-agent-kit/sample
```

The script copies the sample to a temporary folder, breaks it, and calls the tools over MCP exactly as an agent does.
It prints the table and leaves the raw outputs in the temporary folder. They contain local paths, so do not commit them.

## Install

It is not on NuGet yet. Build the package and install it as a global dotnet tool (needs the .NET 10 SDK):

```
git clone https://github.com/robmardeveloper/dotnet-agent-tools
cd dotnet-agent-tools
dotnet pack src/DotnetAgentTools -c Release -o artifacts
dotnet tool install --global DotnetAgentTools --add-source artifacts
```

The server works inside one folder, the workspace: every path the agent passes must be inside it. By default it is the
folder the server starts in; `--Workspace:Root <folder>` sets it.

### Claude Code

From the folder of your solution:

```
claude mcp add --scope project dotnet-agent-tools -- dotnet-agent-tools
```

### VS Code

Add `.vscode/mcp.json` to your solution:

```json
{
  "servers": {
    "dotnet-agent-tools": {
      "type": "stdio",
      "command": "dotnet-agent-tools",
      "args": ["--Workspace:Root", "${workspaceFolder}"]
    }
  }
}
```

## Limits

- **It runs your project's code.** Building runs MSBuild targets and tests run test code, as `dotnet build` and
  `dotnet test` do. Use it only on code you trust.
- **`test` needs Microsoft.Testing.Platform and xUnit v3.** The workspace's `global.json` must set
  `"test": { "runner": "Microsoft.Testing.Platform" }`. On VSTest the tool says so and does not run. VSTest support is
  planned for the next version.
- **One command at a time.** If the agent calls both tools at once, the second waits: two builds of the same project
  at the same time fight over the same files.
- **Each dotnet command stops after 5 minutes.** `test` runs two commands, the build and the tests, so it can take
  up to twice that.
- **Build messages are in English**, so they read the same on every machine. Tests run in the machine's language,
  because tests may depend on it.
- The arguments are checked before they reach `dotnet`: the configuration must be a plain name, and a filter cannot
  start with `-`.

## Other tools

There are bigger MCP servers for .NET, such as dotnet-mcp, dotnet-build-test-mcp, dotnetstaticanalysismcp and
dotnet-test-mcp. Some cover the whole CLI; others add analysis. This one is small on purpose: two tools, short
answers, and a measurement you can repeat.

## How it fits with dotnet-agent-kit

[dotnet-agent-kit](https://github.com/robmardeveloper/dotnet-agent-kit) gives an agent rules and skills for .NET
work. The rules tell the agent what to do; these tools give it just the information it needs to check it.

## Development

```
dotnet build
dotnet test --solution DotnetAgentTools.slnx
```

The tests build and test small projects in `tests/fixtures`, each one broken in a known way.

## License

MIT
