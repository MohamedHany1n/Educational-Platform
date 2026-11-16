using EducationalPlatform.Web.Models;
using EducationalPlatform.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlatform.Web.Controllers;

/// <summary>Pages that any logged-in user (all roles) can use for their own account.</summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, Translate(error));
            }
            return View(model);
        }

        // Keep the user logged in with the new password.
        await _signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToAction(nameof(ChangePassword));
    }

    private static string Translate(IdentityError error) => error.Code switch
    {
        "PasswordMismatch" => "كلمة المرور الحالية غير صحيحة.",
        "PasswordTooShort" => "كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف.",
        "PasswordRequiresDigit" => "كلمة المرور الجديدة يجب أن تحتوي على رقم واحد على الأقل.",
        "PasswordRequiresLower" => "كلمة المرور الجديدة يجب أن تحتوي على حرف إنجليزي صغير (a-z).",
        _ => error.Description
    };
}
