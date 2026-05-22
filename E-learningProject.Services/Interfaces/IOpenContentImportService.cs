namespace E_learningProject.Services.Interfaces;

public interface IOpenContentImportService
{
    Task<OpenContentImportResult> ImportAsync(int maxModules = 20, CancellationToken cancellationToken = default);
}
