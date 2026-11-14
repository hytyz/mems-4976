using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Models;
using MunicipalElections.Services;

namespace MunicipalElections.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly MunicipalityAccessService _access;

    public HomeController(ApplicationDbContext context, MunicipalityAccessService access)
    {
        _context = context;
        _access = access;
    }

    public async Task<IActionResult> Index()
    {
        // A signed-in municipality admin only sees their own municipality.
        var scope = await _access.GetRestrictedScopeAsync(User);

        var municipalities = await _context.Municipalities
            .Where(m => scope == null || scope.Contains(m.MunicipalityId))
            .Include(m => m.Positions)
                .ThenInclude(p => p.Candidates)
            .OrderBy(m => m.ElectionDate)
            .ThenBy(m => m.Name)
            .ToListAsync();

        return View(municipalities);
    }

    public async Task<IActionResult> Municipality(int id)
    {
        var municipality = await _context.Municipalities
            .Include(m => m.Positions)
                .ThenInclude(p => p.Candidates)
            .FirstOrDefaultAsync(m => m.MunicipalityId == id);

        var scope = await _access.GetRestrictedScopeAsync(User);
        if (municipality == null || (scope != null && !scope.Contains(municipality.MunicipalityId)))
        {
            return NotFound();
        }

        ViewData["CanUseGuide"] = User.Identity?.IsAuthenticated != true;
        return View(municipality);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
