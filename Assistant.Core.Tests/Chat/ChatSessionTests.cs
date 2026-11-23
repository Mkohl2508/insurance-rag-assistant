using Assistant.Core.Chat;
using Microsoft.Extensions.AI;

namespace Assistant.Core.Tests.Chat;

public class ChatSessionTests
{
    [Fact]
    public async Task SendAsync_ReturnsTextAndMetadata()
    {
        var client = new FakeChatClient().Returns("Hello!", inputTokens: 12, outputTokens: 3, model: "m1");
        var session = new ChatSession(client);

        var result = await session.SendAsync("Hi");

        Assert.Equal("Hello!", result.Text);
        Assert.Equal("m1", result.Model);
        Assert.Equal(new TokenUsage(12, 3, 15), result.Usage);
        Assert.Equal("stop", result.FinishReason);
    }

    [Fact]
    public async Task SendAsync_SendsFullHistoryOnFollowUp()
    {
        var client = new FakeChatClient().Returns("A1").Returns("A2");
        var session = new ChatSession(client);

        await session.SendAsync("Q1");
        await session.SendAsync("Q2");

        var secondCall = client.ReceivedMessages[1];
        Assert.Equal(["Q1", "A1", "Q2"], secondCall.Select(m => m.Text));
        Assert.Equal(4, session.History.Count);
    }

    [Fact]
    public async Task TotalUsage_SumsAllRequests()
    {
        var client = new FakeChatClient()
            .Returns("A1", inputTokens: 10, outputTokens: 5)
            .Returns("A2", inputTokens: 20, outputTokens: 7);
        var session = new ChatSession(client);

        await session.SendAsync("Q1");
        await session.SendAsync("Q2");

        Assert.Equal(new TokenUsage(30, 12, 42), session.TotalUsage);
    }

    [Fact]
    public async Task SendAsync_OnFailure_RemovesPromptFromHistory()
    {
        var client = new FakeChatClient().Throws(new HttpRequestException("boom"));
        var session = new ChatSession(client, new ChatSessionOptions { SystemPrompt = "sys" });

        await Assert.ThrowsAsync<HttpRequestException>(() => session.SendAsync("Q1"));

        var only = Assert.Single(session.History);
        Assert.Equal(ChatRole.System, only.Role);
        Assert.Equal(TokenUsage.Empty, session.TotalUsage);
    }

    [Fact]
    public async Task Reset_ClearsHistoryAndUsage_ButKeepsSystemPrompt()
    {
        var client = new FakeChatClient().Returns("A1");
        var session = new ChatSession(client, new ChatSessionOptions { SystemPrompt = "sys" });
        await session.SendAsync("Q1");

        session.Reset();

        var only = Assert.Single(session.History);
        Assert.Equal(ChatRole.System, only.Role);
        Assert.Equal("sys", only.Text);
        Assert.Equal(TokenUsage.Empty, session.TotalUsage);
    }

    [Fact]
    public async Task Options_ArePassedToClient()
    {
        var client = new FakeChatClient().Returns("A1");
        var session = new ChatSession(client, new ChatSessionOptions { Temperature = 0.2f, MaxOutputTokens = 100 });

        await session.SendAsync("Q1");

        var options = client.ReceivedOptions[0];
        Assert.NotNull(options);
        Assert.Equal(0.2f, options.Temperature);
        Assert.Equal(100, options.MaxOutputTokens);
    }
}
