namespace E_learningProject.Core.Entities;

public class ContentImportLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "Module";
    public int EntityId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceLicense { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}