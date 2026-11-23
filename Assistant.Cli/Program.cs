using Assistant.Cli;
using Assistant.Core.Chat;
using Microsoft.Extensions.Logging;
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

// Logs go to stderr, one line per entry, so they stay distinguishable from the chat output.
using var loggerFactory = LoggerFactory.Create(builder => builder
    .AddConfiguration(config.Logging)
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));

await new ChatRepl(session, config.OpenAI, config.Documents, loggerFactory).RunAsync();
return 0;
