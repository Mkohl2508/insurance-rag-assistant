namespace Assistant.Core.Documents;

/// <summary>
/// The extracted text of a single PDF page together with its source metadata.
/// </summary>
/// <param name="FileName">File name without directory, e.g. "vgb-2022.pdf".</param>
/// <param name="PageNumber">1-based page number within the file.</param>
/// <param name="Text">Extracted text content of the page.</param>
public sealed record DocumentPage(string FileName, int PageNumber, string Text);
