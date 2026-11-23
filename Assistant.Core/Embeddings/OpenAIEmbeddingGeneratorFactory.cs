using Assistant.Core.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Assistant.Core.Embeddings;

/// <summary>
/// Creates the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for the configured embedding model.
/// Transient failures (429, 5xx, timeouts) are retried by the OpenAI SDK's default pipeline.
/// </summary>
public static class OpenAIEmbeddingGeneratorFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> Create(OpenAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:ApiKey' is not set.");
        if (string.IsNullOrWhiteSpace(options.EmbeddingModel))
            throw new InvalidOperationException($"'{OpenAIOptions.SectionName}:EmbeddingModel' is not set.");

        return new OpenAIClient(options.ApiKey)
            .GetEmbeddingClient(options.EmbeddingModel)
            .AsIEmbeddingGenerator();
    }
}
