using Assistant.Core.Chat;
using Assistant.Core.Configuration;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

var options = config.GetSection(OpenAIOptions.SectionName).Get<OpenAIOptions>()
              ?? throw new InvalidOperationException($"Sektion '{OpenAIOptions.SectionName}' fehlt in der Konfiguration.");

var sessionOptions = config.GetSection(ChatSessionOptions.SectionName).Get<ChatSessionOptions>();

var session = new ChatSession(OpenAIChatClientFactory.Create(options), sessionOptions);

Console.WriteLine($"Chat mit {options.ChatModel}. '/reset' leert den Verlauf, '/exit' beendet.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (input is null) break;

    var command = input.Trim();
    if (command == "/exit") break;
    if (command.Length == 0) continue;
    if (command == "/reset")
    {
        session.Reset();
        Console.WriteLine("Verlauf zurückgesetzt.");
        continue;
    }

    try
    {
        var result = await session.SendAsync(input);
        Console.WriteLine(result.Text);
        Console.WriteLine(
            $"[{result.Model} | in {result.Usage.InputTokens} / out {result.Usage.OutputTokens} " +
            $"| {result.Duration.TotalSeconds:F1}s | Session Σ {session.TotalUsage.TotalTokens} Tokens]");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fehler: {ex.Message}");
    }
}
