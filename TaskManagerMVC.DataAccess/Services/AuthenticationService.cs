using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Data;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.ViewModels;

namespace TaskManagerMVC.DataAccess.Services
{
    public interface IAuthenticationService
    {
        ApplicationUser? ValidateUser(string username, string password);
        ApplicationUser? RegisterUser(RegisterViewModel model);
        ApplicationUser? GetUserById(int userId);
        ApplicationUser? GetUserByUsername(string username);
        bool UsernameExists(string username);
        bool EmailExists(string email);
        bool ChangePassword(int userId, string currentPassword, string newPassword);
        void UpdateLastLogin(int userId);
        List<ApplicationUser> GetAllUsers();

        // Google OAuth Methods
        ApplicationUser? GetUserByEmail(string email);
        ApplicationUser? GetUserByGoogleId(string googleId);
        ApplicationUser? RegisterGoogleUser(RegisterViewModel model, string googleId);
        void UpdateGoogleId(int userId, string googleId);


    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHelper _passwordHelper;

        public AuthenticationService(AppDbContext context, IPasswordHelper passwordHelper)
        {
            _context = context;
            _passwordHelper = passwordHelper;
        }

        // ==================== TRADITIONAL AUTHENTICATION ====================

        public ApplicationUser? ValidateUser(string username, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.UserName == username && u.IsActive);  // ✅ Already correct

            if (user == null)
                return null;

            if (string.IsNullOrEmpty(user.PasswordHash))
                return null;

            if (!_passwordHelper.VerifyPassword(password, user.PasswordHash))
                return null;

            UpdateLastLogin(user.Id);  // ✅ Changed from UserId to Id

            return user;
        }

        public ApplicationUser? RegisterUser(RegisterViewModel model)
        {
            if (UsernameExists(model.Username) || EmailExists(model.Email))
                return null;
            // ==================== VALIDATE PASSWORD STRENGTH ====================
            var passwordValidation = _passwordHelper.ValidatePasswordStrength(model.Password, model.Username);
            if (!passwordValidation.IsValid)
            {
                // Password doesn't meet requirements - return null
                // Errors are available in passwordValidation.Errors
                return null;
            }
            var user = new ApplicationUser
            {
                UserName = model.Username,          // ✅ Changed from Username to UserName
                Email = model.Email,
                PasswordHash = _passwordHelper.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,              // ✅ NEW - Added
                City = model.City,                  // ✅ NEW - Added
                PostalCode = model.PostalCode,      // ✅ NEW - Added
                Role = model.Role ?? "Employee",
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public ApplicationUser? GetUserById(int userId)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Include(u => u.CreatedTasks)
                .FirstOrDefault(u => u.Id == userId);  // ✅ Changed from UserId to Id
        }

        public ApplicationUser? GetUserByUsername(string username)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Include(u => u.CreatedTasks)
                .FirstOrDefault(u => u.UserName == username);  // ✅ Changed from Username to UserName
        }

        public bool UsernameExists(string username)
        {
            return _context.Users.Any(u => u.UserName != null && u.UserName.ToLower() == username.ToLower());  // ✅ Changed
        }

        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.Email != null && u.Email.ToLower() == email.ToLower());  // ✅ Already correct
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);  // ✅ Changed from UserId to Id
            if (user == null)
                return false;

            if (string.IsNullOrEmpty(user.PasswordHash))
                return false;

            if (!_passwordHelper.VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = _passwordHelper.HashPassword(newPassword);
            _context.SaveChanges();

            return true;
        }

        public void UpdateLastLogin(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);  // ✅ Already correct
            if (user != null)
            {
                user.LastLoginDate = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public List<ApplicationUser> GetAllUsers()
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Where(u => u.IsActive)
                .OrderBy(u => u.UserName)  // ✅ Changed from Username to UserName
                .ToList();
        }

        // ==================== GOOGLE OAUTH METHODS ====================

        public ApplicationUser? GetUserByEmail(string email)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Include(u => u.CreatedTasks)
                .FirstOrDefault(u => u.Email != null && u.Email.ToLower() == email.ToLower());  // ✅ Already correct
        }

        public ApplicationUser? GetUserByGoogleId(string googleId)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Include(u => u.CreatedTasks)
                .FirstOrDefault(u => u.GoogleId == googleId);  // ✅ Already correct
        }

        public ApplicationUser? RegisterGoogleUser(RegisterViewModel model, string googleId)
        {
            if (UsernameExists(model.Username))
                return null;

            if (EmailExists(model.Email))
                return null;

            var user = new ApplicationUser
            {
                UserName = model.Username,          // ✅ Changed from Username to UserName
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender ?? "Prefer not to say",  // ✅ NEW - Default for Google users
                City = model.City ?? "Not specified",          // ✅ NEW - Default for Google users
                PostalCode = model.PostalCode ?? "N/A",        // ✅ NEW - Default for Google users
                Role = model.Role ?? "Employee",
                GoogleId = googleId,
                IsActive = true,
                CreatedDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                PasswordHash = null
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public void UpdateGoogleId(int userId, string googleId)
        {
            var user = _context.Users.Find(userId);  // ✅ Find() uses primary key, so this is fine
            if (user != null)
            {
                user.GoogleId = googleId;
                user.LastLoginDate = DateTime.Now;
                _context.SaveChanges();
            }
        }
    }
}