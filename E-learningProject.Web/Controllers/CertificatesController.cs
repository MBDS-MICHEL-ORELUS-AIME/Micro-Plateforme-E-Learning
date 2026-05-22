using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class CertificatesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public CertificatesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Verify(string? code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim();

        var viewModel = new CertificateVerificationViewModel
        {
            SearchCode = normalizedCode,
            Searched = !string.IsNullOrWhiteSpace(normalizedCode)
        };

        if (!viewModel.Searched)
        {
            return View(viewModel);
        }

        var certificate = await _dbContext.Certificates
            .AsNoTracking()
            .Include(c => c.Module)
            .FirstOrDefaultAsync(c => c.UniqueCode == normalizedCode, cancellationToken);

        if (certificate is null)
        {
            viewModel.IsValid = false;
            return View(viewModel);
        }

        viewModel.IsValid = true;
        viewModel.CertificateCode = certificate.UniqueCode;
        viewModel.StudentId = certificate.StudentId;
        viewModel.ModuleTitle = certificate.Module?.Title;
        viewModel.IssueDate = certificate.IssueDate;

        return View(viewModel);
    }
}
