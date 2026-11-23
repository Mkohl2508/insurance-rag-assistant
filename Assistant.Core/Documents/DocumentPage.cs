namespace Assistant.Core.Documents;

/// <summary>
/// The extracted text of a single page of a <see cref="SourceDocument"/>.
/// </summary>
/// <param name="PageNumber">1-based page number within the file.</param>
/// <param name="Text">Extracted text content of the page.</param>
public sealed record DocumentPage(int PageNumber, string Text);
