using DotnetAgentTools.Build;
using DotnetAgentTools.Processes;
using DotnetAgentTools.Testing;
using DotnetAgentTools.Workspace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP messages, so every log goes to stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.Configure<WorkspaceOptions>(builder.Configuration.GetSection(WorkspaceOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkspacePaths>();
builder.Services.AddSingleton<IDotnetCli, DotnetCli>();
builder.Services.AddSingleton<OneAtATime>();
builder.Services.AddSingleton<BuildRunner>();
builder.Services.AddSingleton<TestRunner>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<BuildTool>()
    .WithTools<TestTool>();

await builder.Build().RunAsync();
