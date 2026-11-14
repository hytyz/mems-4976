using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Models;
using MunicipalElections.Services;

namespace MunicipalElections.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = Policies.AdminArea)]
public class MunicipalitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ImageFileService _images;
    private readonly MunicipalityAccessService _access;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public MunicipalitiesController(
        ApplicationDbContext context,
        ImageFileService images,
        MunicipalityAccessService access,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _images = images;
        _access = access;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var municipalityIds = await _access.GetMunicipalityIdsAsync(User);
        return View(await _context.Municipalities
            .Where(m => municipalityIds.Contains(m.MunicipalityId))
            .OrderBy(m => m.Name)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var municipality = await _context.Municipalities
            .Include(m => m.Positions)
            .FirstOrDefaultAsync(m => m.MunicipalityId == id);
        if (municipality == null || !await _access.CanManageMunicipalityAsync(User, municipality.MunicipalityId))
        {
            return NotFound();
        }

        ViewData["Admins"] = (await _userManager.GetUsersInRoleAsync(municipality.AdminRoleName))
            .Select(u => u.UserName ?? string.Empty)
            .OrderBy(n => n)
            .ToList();

        return View(municipality);
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> Create(
        [Bind("Name,Province,ElectionDate,Description")] Municipality municipality,
        IFormFile? logoFile,
        string? adminUserName,
        string? adminPassword)
    {
        ModelState.Remove(nameof(Municipality.LogoFileName));
        ModelState.Remove(nameof(Municipality.AdminRoleName));

        adminUserName = adminUserName?.Trim();
        ViewData["AdminUserName"] = adminUserName;
        bool createAdmin = !string.IsNullOrEmpty(adminUserName) || !string.IsNullOrEmpty(adminPassword);

        if (ModelState.IsValid)
        {
            municipality.Name = municipality.Name.Trim();
            municipality.AdminRoleName = Roles.ForMunicipality(municipality.Name);

            if (await _context.Municipalities.AnyAsync(m => m.Name == municipality.Name) ||
                await _roleManager.RoleExistsAsync(municipality.AdminRoleName))
            {
                ModelState.AddModelError(nameof(Municipality.Name), "A municipality with this name already exists.");
            }

            if (createAdmin)
            {
                await ValidateNewAdminAsync(adminUserName, adminPassword);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(municipality);
        }

        municipality.LogoFileName = _images.Save(logoFile, "municipalities", out string? error);
        if (error != null)
        {
            ModelState.AddModelError(nameof(municipality.LogoFileName), error);
            return View(municipality);
        }

        _context.Add(municipality);
        await _context.SaveChangesAsync();
        await _roleManager.CreateAsync(new IdentityRole(municipality.AdminRoleName));

        if (createAdmin)
        {
            var result = await UsersController.CreateAdminAsync(_userManager, adminUserName!, adminPassword!, municipality.AdminRoleName);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{municipality.Name} was created with administrator '{adminUserName}'.";
            }
            else
            {
                TempData["Error"] = $"{municipality.Name} was created, but its administrator could not be: " +
                                    string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }
        else
        {
            TempData["Success"] = $"{municipality.Name} was created. Add an administrator for it under Users.";
        }

        return RedirectToAction(nameof(Details), new { id = municipality.MunicipalityId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var municipality = await _context.Municipalities.FindAsync(id);
        if (municipality == null || !await _access.CanManageMunicipalityAsync(User, municipality.MunicipalityId))
        {
            return NotFound();
        }

        return View(municipality);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MunicipalityId,Name,Province,ElectionDate,Description")] Municipality municipality, IFormFile? logoFile, bool removeLogo = false)
    {
        if (id != municipality.MunicipalityId)
        {
            return NotFound();
        }

        var existing = await _context.Municipalities.FindAsync(id);
        if (existing == null || !await _access.CanManageMunicipalityAsync(User, id))
        {
            return NotFound();
        }

        ModelState.Remove(nameof(Municipality.LogoFileName));
        ModelState.Remove(nameof(Municipality.AdminRoleName));
        municipality.LogoFileName = existing.LogoFileName;

        string newName = (municipality.Name ?? string.Empty).Trim();
        bool renamed = ModelState.IsValid && newName != existing.Name;
        string newRoleName = Roles.ForMunicipality(newName);

        if (renamed &&
            (await _context.Municipalities.AnyAsync(m => m.Name == newName && m.MunicipalityId != id) ||
             (!string.Equals(newRoleName, existing.AdminRoleName, StringComparison.OrdinalIgnoreCase) &&
              await _roleManager.RoleExistsAsync(newRoleName))))
        {
            ModelState.AddModelError(nameof(Municipality.Name), "A municipality with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(municipality);
        }

        if (removeLogo)
        {
            _images.Delete(existing.LogoFileName, "municipalities");
            existing.LogoFileName = null;
        }
        else if (logoFile != null && logoFile.Length > 0)
        {
            string? fileName = _images.Save(logoFile, "municipalities", out string? error);
            if (error != null)
            {
                ModelState.AddModelError(nameof(Municipality.LogoFileName), error);
                return View(municipality);
            }

            _images.Delete(existing.LogoFileName, "municipalities");
            existing.LogoFileName = fileName;
        }

        if (renamed)
        {
            var role = await _roleManager.FindByNameAsync(existing.AdminRoleName);
            if (role != null)
            {
                await _roleManager.SetRoleNameAsync(role, newRoleName);
                await _roleManager.UpdateAsync(role);
            }
            else
            {
                await _roleManager.CreateAsync(new IdentityRole(newRoleName));
            }

            existing.AdminRoleName = newRoleName;
        }

        existing.Name = newName;
        existing.Province = municipality.Province;
        existing.ElectionDate = municipality.ElectionDate;
        existing.Description = municipality.Description;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"{existing.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var municipality = await _context.Municipalities
            .FirstOrDefaultAsync(m => m.MunicipalityId == id);
        if (municipality == null)
        {
            return NotFound();
        }

        return View(municipality);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var municipality = await _context.Municipalities
            .Include(m => m.Positions)
                .ThenInclude(p => p.Candidates)
            .FirstOrDefaultAsync(m => m.MunicipalityId == id);
        if (municipality != null)
        {
            _images.Delete(municipality.LogoFileName, "municipalities");
            foreach (var candidate in municipality.Positions.SelectMany(p => p.Candidates))
            {
                _images.Delete(candidate.PhotoFileName, "candidates");
            }

            _context.Municipalities.Remove(municipality);
            await _context.SaveChangesAsync();

            var role = await _roleManager.FindByNameAsync(municipality.AdminRoleName);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
            }

            TempData["Success"] = $"{municipality.Name} was deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateNewAdminAsync(string? userName, string? password)
    {
        if (string.IsNullOrEmpty(userName))
        {
            ModelState.AddModelError("adminUserName", "Enter a username for the administrator (or clear the password).");
        }
        else if (userName.Length is < 2 or > 50 ||
                 !userName.All(ch => char.IsAsciiLetterOrDigit(ch) || "._@+-".Contains(ch)))
        {
            ModelState.AddModelError("adminUserName", "Username must be 2-50 characters: letters, digits and . _ @ + -");
        }
        else if (await _userManager.FindByNameAsync(userName) != null)
        {
            ModelState.AddModelError("adminUserName", $"Username '{userName}' is already taken.");
        }

        if (string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("adminPassword", "Enter a password for the administrator.");
        }
        else
        {
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, new ApplicationUser { UserName = userName }, password);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("adminPassword", error.Description);
                }
            }
        }
    }
}
