using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Services;

namespace MunicipalElections.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = Policies.AdminArea)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly MunicipalityAccessService _access;

    public DashboardController(ApplicationDbContext context, MunicipalityAccessService access)
    {
        _context = context;
        _access = access;
    }

    public async Task<IActionResult> Index()
    {
        // Everything on the dashboard is limited to the municipalities this user administers.
        var municipalityIds = await _access.GetMunicipalityIdsAsync(User);
        bool isSuperAdmin = _access.IsSuperAdmin(User);

        var municipalities = await _context.Municipalities
            .Where(m => municipalityIds.Contains(m.MunicipalityId))
            .Include(m => m.Positions)
                .ThenInclude(p => p.Candidates)
            .OrderBy(m => m.Name)
            .ToListAsync();

        ViewData["MunicipalityCount"] = municipalities.Count;
        ViewData["PositionCount"] = municipalities.Sum(m => m.Positions.Count);
        ViewData["CandidateCount"] = municipalities.Sum(m => m.Positions.Sum(p => p.Candidates.Count));
        ViewData["IsSuperAdmin"] = isSuperAdmin;
        if (isSuperAdmin)
        {
            ViewData["UserCount"] = await _context.Users.CountAsync();
        }

        return View(municipalities);
    }
}
