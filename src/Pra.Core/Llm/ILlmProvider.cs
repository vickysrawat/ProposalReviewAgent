namespace Pra.Core.Llm;

/// <summary>Task classes used for model routing: cheaper models for parsing, stronger for solutioning and writing.</summary>
public enum ModelTaskClass
{
    Parsing,
    Extraction,
    Solutioning,
    Writing,
    Review,
}

/// <summary>A single completion request routed through the gateway-approved providers.</summary>
public sealed record CompletionRequest
{
    public required string AgentId { get; init; }
    public required ModelTaskClass TaskClass { get; init; }
    public required string Prompt { get; init; }

    /// <summary>Untrusted retrieved content, wrapped as data — never as instructions.</summary>
    public IReadOnlyList<string> ContextBlocks { get; init; } = [];
}

public sealed record CompletionResult
{
    public required string Content { get; init; }
    public required string ModelDeployment { get; init; }
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
}

/// <summary>
/// PRA-Core LLM abstraction. Implementations route through the APIM AI
/// gateway so every model call carries the agent identity, is rate/cost
/// limited, and is traced.
/// </summary>
public interface ILlmProvider
{
    Task<CompletionResult> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default);
}
