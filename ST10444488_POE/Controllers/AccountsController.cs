using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ST10444488_POE.Models;
using System.Security.Cryptography;
using System.Text;

namespace ST10444488_POE.Controllers
{
    public class AccountsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountsController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Register() => View();
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email, string phoneNumber, string role)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var user = new User
            {
                UserName = username,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = role
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return RedirectToAction("Login");
            }

            ViewBag.Error = string.Join("; ", result.Errors.Select(e => e.Description));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Username, string Password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var user = await _userManager.FindByNameAsync(Username);
            if (user == null)
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }
    }
}