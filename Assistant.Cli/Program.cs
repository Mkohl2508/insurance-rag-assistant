using Assistant.Core.Chat;
using Assistant.Core.Configuration;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

var options = config.GetSection(OpenAIOptions.SectionName).Get<OpenAIOptions>()
              ?? throw new InvalidOperationException($"Section '{OpenAIOptions.SectionName}' is missing from the configuration.");

var sessionOptions = config.GetSection(ChatSessionOptions.SectionName).Get<ChatSessionOptions>();

var session = new ChatSession(OpenAIChatClientFactory.Create(options), sessionOptions);

Console.WriteLine($"Chatting with {options.ChatModel}. '/reset' clears the history, '/exit' quits.");

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
        Console.WriteLine("History cleared.");
        continue;
    }

    try
    {
        var result = await session.SendAsync(input);
        Console.WriteLine(result.Text);
        Console.WriteLine(
            $"[{result.Model} | in {result.Usage.InputTokens} / out {result.Usage.OutputTokens} " +
            $"| {result.Duration.TotalSeconds:F1}s | session total {session.TotalUsage.TotalTokens} tokens]");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
