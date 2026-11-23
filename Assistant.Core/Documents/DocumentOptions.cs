namespace Assistant.Core.Documents;

/// <summary>
/// Where source documents are loaded from. Bound to the "Documents" section.
/// </summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    /// <summary>Directory containing the PDF files. The CLI resolves relative paths against the application's base directory (where appsettings.json lives).</summary>
    public string DataDirectory { get; set; } = "data";
}
