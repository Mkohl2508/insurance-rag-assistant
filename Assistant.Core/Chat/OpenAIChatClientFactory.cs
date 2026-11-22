using Assistant.Core.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Assistant.Core.Chat;

/// <summary>
/// Erzeugt den <see cref="IChatClient"/>. Einziger Ort, der das OpenAI-SDK direkt kennt.
/// </summary>
public static class OpenAIChatClientFactory
{
    public static IChatClient Create(OpenAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:ApiKey' ist nicht gesetzt.");
        if (string.IsNullOrWhiteSpace(options.ChatModel))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:ChatModel' ist nicht gesetzt.");

        return new OpenAIClient(options.ApiKey)
            .GetChatClient(options.ChatModel)
            .AsIChatClient();
    }
}
