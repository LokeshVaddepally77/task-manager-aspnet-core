using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using TaskManagerMVC.DataAccess.ViewModels;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.Web.Filters;
using IAuthService = TaskManagerMVC.DataAccess.Services.IAuthenticationService;

namespace TaskManagerMVC.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Dashboard", "Task");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _authService.ValidateUser(model.Username, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact administrator.");
                return View(model);
            }

            // Set session variables
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("FullName", user.FullName);

            TempData["Success"] = $"Welcome back, {user.FullName}!";

            // Redirect to return URL or dashboard 

            //if (!string.IsNullOrEmpty(model.ReturnUrl))
            //{
            //    return Redirect(model.ReturnUrl);
            //}
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Dashboard", "Task");
        }

        // ==================== GOOGLE OAUTH LOGIN ====================

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { returnUrl }),
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            try
            {
                // Authenticate the user from Google
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded)
                {
                    TempData["Error"] = "Google authentication failed. Please try again.";
                    return RedirectToAction("Login");
                }

                // Extract user information from Google claims
                var claims = authenticateResult.Principal?.Claims;
                if (claims == null)
                {
                    TempData["Error"] = "Unable to retrieve user information from Google.";
                    return RedirectToAction("Login");
                }

                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var givenName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
                var surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    TempData["Error"] = "Unable to retrieve required user information from Google.";
                    return RedirectToAction("Login");
                }

                // Check if user exists by email or GoogleId
                var existingUser = _authService.GetUserByEmail(email) ?? _authService.GetUserByGoogleId(googleId);

                if (existingUser != null)
                {
                    // Existing user - update GoogleId if not set
                    if (string.IsNullOrEmpty(existingUser.GoogleId))
                    {
                        _authService.UpdateGoogleId(existingUser.UserId, googleId);
                    }

                    // Check if user is active
                    if (!existingUser.IsActive)
                    {
                        TempData["Error"] = "Your account has been deactivated. Please contact administrator.";
                        return RedirectToAction("Login");
                    }

                    // Set session for existing user
                    HttpContext.Session.SetInt32("UserId", existingUser.UserId);
                    HttpContext.Session.SetString("Username", existingUser.Username);
                    HttpContext.Session.SetString("Role", existingUser.Role);
                    HttpContext.Session.SetString("Email", existingUser.Email ?? "");
                    HttpContext.Session.SetString("FullName", existingUser.FullName);
                    HttpContext.Session.SetString("GoogleId", googleId);
                    HttpContext.Session.SetString("LoginMethod", "Google");

                    TempData["Success"] = $"Welcome back, {existingUser.FullName}!";
                }
                else
                {
                    // New Google user - create account automatically
                    var registerModel = new RegisterViewModel
                    {
                        Username = email.Split('@')[0], // Use email prefix as username
                        Email = email,
                        FirstName = givenName ?? name?.Split(' ').FirstOrDefault() ?? "User",
                        LastName = surname ?? name?.Split(' ').LastOrDefault() ?? "",
                        Password = Guid.NewGuid().ToString(), // Random password (won't be used)
                        ConfirmPassword = Guid.NewGuid().ToString(),
                        Role = "Employee" // Default role for Google users
                    };

                    // Check if username exists, if so, append random number
                    if (_authService.UsernameExists(registerModel.Username))
                    {
                        registerModel.Username = $"{registerModel.Username}{new Random().Next(1000, 9999)}";
                    }

                    var newUser = _authService.RegisterGoogleUser(registerModel, googleId);

                    if (newUser == null)
                    {
                        TempData["Error"] = "Failed to create your account. Please try again or contact administrator.";
                        return RedirectToAction("Login");
                    }

                    // Set session for new user
                    HttpContext.Session.SetInt32("UserId", newUser.UserId);
                    HttpContext.Session.SetString("Username", newUser.Username);
                    HttpContext.Session.SetString("Role", newUser.Role);
                    HttpContext.Session.SetString("Email", newUser.Email ?? "");
                    HttpContext.Session.SetString("FullName", newUser.FullName);
                    HttpContext.Session.SetString("GoogleId", googleId);
                    HttpContext.Session.SetString("LoginMethod", "Google");

                    TempData["Success"] = $"Welcome to Task Manager, {newUser.FullName}! Your account has been created successfully.";
                }

                // Handle return URL with open redirect protection
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Dashboard", "Task");
            }
            catch (Exception ex)
            {
                // Log the exception (add logging service if needed)
                Console.WriteLine($"Google authentication error: {ex.Message}");
                TempData["Error"] = "An error occurred during Google authentication. Please try again.";
                return RedirectToAction("Login");
            }
        }

        // ==================== REGISTER ====================

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Dashboard", "Task");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_authService.UsernameExists(model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            if (_authService.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(model);
            }

            // Force Employee role for public registration
            model.Role = "Employee";

            var user = _authService.RegisterUser(model);

            if (user == null)
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }

            TempData["Success"] = "Registration successful! Please login with your credentials.";
            return RedirectToAction("Login");
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Sign out from cookie authentication (for Google OAuth)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Sign out from cookie authentication (for Google OAuth)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // ==================== ACCESS DENIED ====================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== PROFILE ====================

        [Authenticate]
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = _authService.GetUserById(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var profile = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                TotalTasks = user.AssignedTasks?.Count ?? 0,
                CompletedTasks = user.AssignedTasks?.Count(t => t.Status == "Completed") ?? 0,
                InProgressTasks = user.AssignedTasks?.Count(t => t.Status == "In Progress") ?? 0
            };
            profile.CompletionRate = profile.TotalTasks > 0
                ? (profile.CompletedTasks * 100.0 / profile.TotalTasks)
                : 0;

            // Add Google login information
            ViewBag.IsGoogleUser = !string.IsNullOrEmpty(HttpContext.Session.GetString("GoogleId"));
            ViewBag.LoginMethod = HttpContext.Session.GetString("LoginMethod") ?? "Traditional";

            return View(profile);
        }

        // ==================== CHANGE PASSWORD ====================

        [Authenticate]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Check if user logged in via Google
            var isGoogleUser = !string.IsNullOrEmpty(HttpContext.Session.GetString("GoogleId"));
            if (isGoogleUser)
            {
                TempData["Info"] = "Google users cannot change password. Your account is secured by Google.";
                return RedirectToAction("Profile");
            }

            return View(new ChangePasswordViewModel());
        }

        [Authenticate]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            // Check if user logged in via Google
            var isGoogleUser = !string.IsNullOrEmpty(HttpContext.Session.GetString("GoogleId"));
            if (isGoogleUser)
            {
                TempData["Error"] = "Google users cannot change password.";
                return RedirectToAction("Profile");
            }

            if (!_authService.ChangePassword(userId.Value, model.CurrentPassword, model.NewPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

        // ==================== CREATE USER (MANAGER ONLY) ====================

        [ManagerOnly]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new RegisterViewModel());
        }

        [ManagerOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_authService.UsernameExists(model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            if (_authService.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(model);
            }

            var user = _authService.RegisterUser(model);

            if (user == null)
            {
                ModelState.AddModelError("", "Failed to create user.");
                return View(model);
            }

            TempData["Success"] = $"User '{model.Username}' created successfully as {model.Role}!";
            return RedirectToAction("Index", "Team");
        }

        // ==================== MANAGE USERS (MANAGER ONLY) ====================

        [ManagerOnly]
        [HttpGet]
        public IActionResult ManageUsers()
        {
            var users = _authService.GetAllUsers();
            return View(users);
        }
    }
}