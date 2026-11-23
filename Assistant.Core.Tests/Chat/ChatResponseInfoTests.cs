using Assistant.Core.Chat;

namespace Assistant.Core.Tests.Chat;

public class ChatResponseInfoTests
{
    [Theory]
    [InlineData("stop", true)]
    [InlineData(null, true)]
    [InlineData("length", false)]
    [InlineData("content_filter", false)]
    public void IsComplete_DependsOnFinishReason(string? finishReason, bool expected)
    {
        var info = new ChatResponseInfo("text", "m1", TokenUsage.Empty, finishReason, TimeSpan.Zero);

        Assert.Equal(expected, info.IsComplete);
    }
}
