using Assistant.Core.Chat;
using Assistant.Core.Configuration;
using Assistant.Core.Documents;
using Microsoft.Extensions.Configuration;

namespace Assistant.Cli;

/// <summary>
/// All settings the CLI needs, bound from appsettings.json and user secrets.
/// </summary>
internal sealed record AppConfiguration(OpenAIOptions OpenAI, ChatSessionOptions? Chat, DocumentOptions Documents)
{
    /// <exception cref="InvalidOperationException">A required section is missing.</exception>
    public static AppConfiguration Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<AppConfiguration>()
            .Build();

        var openAI = config.GetSection(OpenAIOptions.SectionName).Get<OpenAIOptions>()
                     ?? throw new InvalidOperationException($"Section '{OpenAIOptions.SectionName}' is missing from the configuration.");

        var chat = config.GetSection(ChatSessionOptions.SectionName).Get<ChatSessionOptions>();

        var documents = config.GetSection(DocumentOptions.SectionName).Get<DocumentOptions>() ?? new DocumentOptions();

        return new AppConfiguration(openAI, chat, documents);
    }
}
