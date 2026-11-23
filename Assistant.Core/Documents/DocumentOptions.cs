namespace Assistant.Core.Documents;

/// <summary>
/// Where source documents are loaded from. Bound to the "Documents" section.
/// </summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    /// <summary>Directory containing the PDF files. Relative paths are resolved against the working directory.</summary>
    public string DataDirectory { get; set; } = "data";
}
