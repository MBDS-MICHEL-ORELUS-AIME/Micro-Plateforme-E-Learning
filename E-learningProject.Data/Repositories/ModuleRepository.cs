using E_learningProject.Core.Entities;
using E_learningProject.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Data.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ModuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Modules
            .AsNoTracking()
            .Include(m => m.Lessons)
            .OrderBy(m => m.Title)
            .ToListAsync(cancellationToken);
    }
}