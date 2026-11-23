using Assistant.Cli;
using Assistant.Core.Chat;
using System.Text;

// LLM answers can contain non-ASCII characters (quotes, dashes, currency signs).
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

AppConfiguration config;
ChatSession session;

try
{
    config = AppConfiguration.Load();
    session = new ChatSession(OpenAIChatClientFactory.Create(config.OpenAI), config.Chat);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    return 1;
}

await new ChatRepl(session, config.OpenAI, config.Documents).RunAsync();
return 0;
