using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Assistant.Core.Chat;

/// <summary>
/// Keeps the conversation history, sends requests to the LLM and accumulates token usage.
/// </summary>
public sealed class ChatSession
{
    private readonly IChatClient _client;
    private readonly ChatOptions _chatOptions;
    private readonly string? _systemPrompt;
    private readonly List<ChatMessage> _history = [];

    public ChatSession(IChatClient client, ChatSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        options ??= new ChatSessionOptions();

        _client = client;
        _systemPrompt = options.SystemPrompt;
        _chatOptions = new ChatOptions
        {
            Temperature = options.Temperature,
            MaxOutputTokens = options.MaxOutputTokens,
        };

        Reset();
    }

    public IReadOnlyList<ChatMessage> History => _history;

    public TokenUsage TotalUsage { get; private set; } = TokenUsage.Empty;

    public async Task<ChatResponseInfo> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        _history.Add(new ChatMessage(ChatRole.User, prompt));

        var stopwatch = Stopwatch.StartNew();
        ChatResponse response;
        try
        {
            response = await _client.GetResponseAsync(_history, _chatOptions, cancellationToken);
        }
        catch
        {
            // Do not keep a failed request in the history.
            _history.RemoveAt(_history.Count - 1);
            throw;
        }
        stopwatch.Stop();

        _history.AddMessages(response);

        var usage = TokenUsage.From(response.Usage);
        TotalUsage += usage;

        return new ChatResponseInfo(
            response.Text,
            response.ModelId,
            usage,
            response.FinishReason?.Value,
            stopwatch.Elapsed);
    }

    public void Reset()
    {
        _history.Clear();
        TotalUsage = TokenUsage.Empty;

        if (!string.IsNullOrWhiteSpace(_systemPrompt))
            _history.Add(new ChatMessage(ChatRole.System, _systemPrompt));
    }
}
