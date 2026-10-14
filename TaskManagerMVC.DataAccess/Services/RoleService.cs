using TaskManagerMVC.DataAccess.Data;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.Services;
using TaskManagerMVC.DataAccess.ViewModels;

namespace TaskManagerMVC.DataAccess.Services
{
    /// <summary>
    /// Role Service Implementation
    /// Implements IRoleService for role-based authorization management
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;

        // Default system roles that cannot be deleted
        private static readonly string[] SystemRoles = { "Manager", "Employee" };

        public RoleService(AppDbContext context)
        {
            _context = context;

            // Ensure default roles exist on startup
            EnsureDefaultRolesExist();
        }

        /// <summary>
        /// Ensure default roles (Manager, Employee) exist in database
        /// </summary>
        private void EnsureDefaultRolesExist()
        {
            try
            {
                foreach (var roleName in SystemRoles)
                {
                    if (!_context.Roles.Any(r => r.RoleName == roleName))
                    {
                        _context.Roles.Add(new Role
                        {
                            RoleName = roleName,
                            Description = roleName == "Manager"
                                ? "Full access to all features including task creation and user management"
                                : "Can view and update assigned tasks only",
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        });
                    }
                }
                _context.SaveChanges();
            }
            catch
            {
                // Ignore errors during initialization (table might not exist yet)
            }
        }

        // ==================== ROLE CRUD OPERATIONS ====================

        /// <summary>
        /// Get all roles with user count
        /// </summary>
        public List<RoleListViewModel> GetAllRoles()
        {
            return _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleListViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    UserCount = _context.Users.Count(u => u.Role == r.RoleName && u.IsActive),
                    CreatedDate = r.CreatedDate,
                    IsSystemRole = SystemRoles.Contains(r.RoleName)
                })
                .ToList();
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        public Role? GetRoleById(int roleId)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleId == roleId && r.IsActive);
        }

        /// <summary>
        /// Get role by name
        /// </summary>
        public Role? GetRoleByName(string roleName)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleName.ToLower() == roleName.ToLower() && r.IsActive);
        }

        /// <summary>
        /// Check if role exists
        /// </summary>
        public bool RoleExists(string roleName)
        {
            return _context.Roles.Any(r => r.RoleName.ToLower() == roleName.ToLower());
        }

        /// <summary>
        /// Create new role
        /// </summary>
        public bool CreateRole(string roleName, string? description = null)
        {
            try
            {
                // Check if role already exists
                if (RoleExists(roleName))
                    return false;

                var role = new Role
                {
                    RoleName = roleName,
                    Description = description,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Roles.Add(role);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update existing role
        /// </summary>
        public bool UpdateRole(int roleId, string roleName, string? description = null)
        {
            try
            {
                var role = GetRoleById(roleId);
                if (role == null)
                    return false;

                // Store old role name for updating users
                var oldRoleName = role.RoleName;

                // Update role properties
                role.RoleName = roleName;
                role.Description = description;

                // If role name changed, update all users with this role
                if (oldRoleName != roleName)
                {
                    var usersWithRole = _context.Users.Where(u => u.Role == oldRoleName).ToList();
                    foreach (var user in usersWithRole)
                    {
                        user.Role = roleName;
                    }
                }

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete role (soft delete - sets IsActive to false)
        /// </summary>
        public bool DeleteRole(int roleId)
        {
            try
            {
                var role = GetRoleById(roleId);
                if (role == null)
                    return false;

                // Cannot delete system roles (Manager, Employee)
                if (SystemRoles.Contains(role.RoleName))
                    return false;

                // Cannot delete role that has users assigned
                if (_context.Users.Any(u => u.Role == role.RoleName && u.IsActive))
                    return false;

                // Soft delete
                role.IsActive = false;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==================== USER-ROLE MANAGEMENT ====================

        /// <summary>
        /// Get all users in a specific role
        /// </summary>
        public List<ApplicationUser> GetUsersInRole(string roleName)
        {
            return _context.Users
                .Where(u => u.Role == roleName && u.IsActive)
                .OrderBy(u => u.UserName)
                .ToList();
        }

        /// <summary>
        /// Get all users NOT in a specific role
        /// </summary>
        public List<ApplicationUser> GetUsersNotInRole(string roleName)
        {
            return _context.Users
                .Where(u => u.Role != roleName && u.IsActive)
                .OrderBy(u => u.UserName)
                .ToList();
        }

        /// <summary>
        /// Add user to role (changes user's role)
        /// </summary>
        public bool AddUserToRole(int userId, string roleName)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                    return false;

                // Verify role exists
                if (!RoleExists(roleName))
                    return false;

                // Update user's role
                user.Role = roleName;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remove user from role (sets user back to Employee role)
        /// </summary>
        public bool RemoveUserFromRole(int userId, string roleName)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null || user.Role != roleName)
                    return false;

                // Set to default Employee role when removed from current role
                user.Role = "Employee";
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get user's current role
        /// </summary>
        public string GetUserRole(int userId)
        {
            var user = _context.Users.Find(userId);
            return user?.Role ?? "Employee";
        }
    }
}
