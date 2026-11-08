namespace DotnetAgentTools.Workspace;

/// <summary>Where the server may work and how long a command may take. Bound from configuration section "Workspace".</summary>
internal sealed class WorkspaceOptions
{
    public const string SectionName = "Workspace";

    /// <summary>The folder the agent works in. Every path must be inside it. Defaults to the current directory.</summary>
    public string? Root { get; set; }

    /// <summary>The longest a single dotnet command may run before it is stopped.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
