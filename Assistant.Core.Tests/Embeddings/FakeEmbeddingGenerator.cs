using Microsoft.Extensions.AI;

namespace Assistant.Core.Tests.Embeddings;

/// <summary>
/// Test double for <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>: by default returns one vector per input
/// whose single value is the input's length. Records the inputs of each call.
/// </summary>
internal sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public List<IReadOnlyList<string>> ReceivedBatches { get; } = [];

    /// <summary>Overrides the default behaviour; receives the inputs of the call.</summary>
    public Func<IReadOnlyList<string>, GeneratedEmbeddings<Embedding<float>>>? Handler { get; set; }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var inputs = values.ToList();
        ReceivedBatches.Add(inputs);

        var result = Handler?.Invoke(inputs)
                     ?? new GeneratedEmbeddings<Embedding<float>>(inputs.Select(v => new Embedding<float>(new float[] { v.Length })));
        return Task.FromResult(result);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
