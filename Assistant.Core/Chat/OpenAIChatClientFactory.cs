using Assistant.Core.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Assistant.Core.Chat;

/// <summary>
/// Creates the <see cref="IChatClient"/>. The only place that knows the OpenAI SDK directly.
/// </summary>
public static class OpenAIChatClientFactory
{
    public static IChatClient Create(OpenAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:ApiKey' is not set.");
        if (string.IsNullOrWhiteSpace(options.ChatModel))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:ChatModel' is not set.");

        return new OpenAIClient(options.ApiKey)
            .GetChatClient(options.ChatModel)
            .AsIChatClient();
    }
}
