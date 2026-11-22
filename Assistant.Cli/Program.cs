using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var model = config["ModelName"];
var key = config["OpenAIKey"];

// Create the IChatClient
var chatClient =
    new OpenAIClient(key).GetChatClient(model).AsIChatClient();


