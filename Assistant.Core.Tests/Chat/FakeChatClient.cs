using Microsoft.Extensions.AI;

namespace Assistant.Core.Tests.Chat;

/// <summary>
/// Test-Double für <see cref="IChatClient"/>: liefert vorbereitete Antworten oder wirft eine Exception
/// und merkt sich, welche Nachrichten bei jedem Aufruf übergeben wurden.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<Func<ChatResponse>> _responses = new();

    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = [];
    public List<ChatOptions?> ReceivedOptions { get; } = [];

    public FakeChatClient Returns(string text, long inputTokens = 10, long outputTokens = 5, string model = "fake-model")
    {
        _responses.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = model,
            FinishReason = ChatFinishReason.Stop,
            Usage = new UsageDetails
            {
                InputTokenCount = inputTokens,
                OutputTokenCount = outputTokens,
                TotalTokenCount = inputTokens + outputTokens,
            },
        });
        return this;
    }

    public FakeChatClient Throws(Exception exception)
    {
        _responses.Enqueue(() => throw exception);
        return this;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Snapshot, da die Session ihre Liste danach weiter verändert.
        ReceivedMessages.Add(messages.ToList());
        ReceivedOptions.Add(options);
        return Task.FromResult(_responses.Dequeue()());
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
