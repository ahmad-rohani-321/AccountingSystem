using AccountingSystem.Models.Identity;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class AuthController(SignInManager<User> signInManager) : Controller
    {
        private readonly SignInManager<User> _signInManager = signInManager;

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginModel loginModel)
        {
            var user = await _signInManager.UserManager.FindByNameAsync(loginModel.UserName);
            if (string.IsNullOrWhiteSpace(loginModel.UserName) || string.IsNullOrWhiteSpace(loginModel.Password))
            {
                ModelState.AddModelError(string.Empty, "پاسورډ او یوزر نوم حتمي دي.");
                return View();
            }
            else if (user is null)
            {
                ModelState.AddModelError(string.Empty, "یوزر نوم یا پاسورډ غلط دی.");
                return View();
            }
            else if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "یوزر غیر فعال دی.");
                return View();
            }
            else
            {
                await _signInManager.PasswordSignInAsync(loginModel.UserName, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: false);
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
