using E_learningProject.Core.Entities;

namespace E_learningProject.Data.Repositories;

public interface IModuleRepository
{
    Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default);
}