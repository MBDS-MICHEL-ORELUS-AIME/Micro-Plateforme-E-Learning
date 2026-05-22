namespace E_learningProject.Web.Models;

public sealed class AdminContentSyncViewModel
{
    public int TotalImportedModules { get; set; }
    public int DistinctSources { get; set; }
    public DateTime? LastImportAt { get; set; }
    public List<ImportSourceItemViewModel> Sources { get; set; } = new();
}

public sealed class ImportSourceItemViewModel
{
    public string SourceName { get; set; } = string.Empty;
    public int ImportsCount { get; set; }
    public DateTime LastImportedAt { get; set; }
}
