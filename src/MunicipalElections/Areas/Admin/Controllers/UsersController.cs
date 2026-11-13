using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Models;
using MunicipalElections.Services;

namespace MunicipalElections.Areas.Admin.Controllers;

/// <summary>
/// Administrator accounts. There is no public registration: only a SuperAdmin
/// can create, remove or reset the password of an administrator.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        string? currentUserId = _userManager.GetUserId(User);

        var memberships = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            select new { userRole.UserId, role.Name }).ToListAsync();

        var users = await _context.Users.OrderBy(u => u.UserName).ToListAsync();

        var model = users.Select(u => new AdminUserListItem
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Groups = memberships.Where(m => m.UserId == u.Id).Select(m => m.Name!).OrderBy(n => n).ToList(),
            IsCurrentUser = u.Id == currentUserId
        }).ToList();

        return View(model);
    }

    public async Task<IActionResult> Create(int? municipalityId)
    {
        var model = new CreateAdminUserViewModel();
        if (municipalityId.HasValue)
        {
            model.RoleName = await _context.Municipalities
                .Where(m => m.MunicipalityId == municipalityId.Value)
                .Select(m => m.AdminRoleName)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        await PopulateGroupsAsync(model.RoleName);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAdminUserViewModel model)
    {
        var allowedRoles = await GetAssignableRolesAsync();
        if (!string.IsNullOrEmpty(model.RoleName) && !allowedRoles.Contains(model.RoleName))
        {
            ModelState.AddModelError(nameof(model.RoleName), "Unknown admin group.");
        }

        if (ModelState.IsValid)
        {
            var result = await CreateAdminAsync(_userManager, model.UserName, model.Password, model.RoleName);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Administrator '{model.UserName}' was created in the group '{model.RoleName}'.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        await PopulateGroupsAsync(model.RoleName);
        return View(model);
    }

    public async Task<IActionResult> ResetPassword(string? id)
    {
        var user = id is null ? null : await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new ResetPasswordViewModel { Id = user.Id, UserName = user.UserName ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        model.UserName = user.UserName ?? string.Empty;
        ModelState.Remove(nameof(model.UserName));

        if (ModelState.IsValid)
        {
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Password for '{user.UserName}' was changed.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Delete(string? id)
    {
        var user = id is null ? null : await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminUserListItem
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Groups = (await _userManager.GetRolesAsync(user)).ToList(),
            IsCurrentUser = user.Id == _userManager.GetUserId(User)
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete the account you are signed in with.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"Administrator '{user.UserName}' was deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Creates an administrator account and adds it to one admin group.</summary>
    internal static async Task<IdentityResult> CreateAdminAsync(
        UserManager<ApplicationUser> userManager, string userName, string password, string roleName)
    {
        var user = new ApplicationUser { UserName = userName.Trim(), EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return result;
        }

        result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            await userManager.DeleteAsync(user);
        }

        return result;
    }

    private async Task<List<string>> GetAssignableRolesAsync()
    {
        var roles = await _context.Municipalities
            .OrderBy(m => m.Name)
            .Select(m => m.AdminRoleName)
            .ToListAsync();
        roles.Add(Roles.SuperAdmin);
        return roles;
    }

    private async Task PopulateGroupsAsync(string? selected)
    {
        ViewData["Groups"] = new SelectList(await GetAssignableRolesAsync(), selected);
    }
}
