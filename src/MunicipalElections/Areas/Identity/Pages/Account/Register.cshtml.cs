#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MunicipalElections.Areas.Identity.Pages.Account
{
    // Public self-registration is disabled: only Super Admin creates municipality
    // admin accounts. This page is kept so Identity's route conventions still
    // resolve /Identity/Account/Register, but it just redirects to Login.
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet(string returnUrl = null)
        {
            return RedirectToPage("./Login", new { returnUrl });
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            return RedirectToPage("./Login", new { returnUrl });
        }
    }
}
