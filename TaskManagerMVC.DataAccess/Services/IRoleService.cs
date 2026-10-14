using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.ViewModels;

namespace TaskManagerMVC.DataAccess.Services
{
    /// <summary>
    /// Role Service Interface
    /// Handles role management operations
    /// </summary>
    public interface IRoleService
    {
        // ==================== ROLE CRUD ====================

        /// <summary>
        /// Get all roles with user count
        /// </summary>
        List<RoleListViewModel> GetAllRoles();

        /// <summary>
        /// Get role by ID
        /// </summary>
        Role? GetRoleById(int roleId);

        /// <summary>
        /// Get role by name
        /// </summary>
        Role? GetRoleByName(string roleName);

        /// <summary>
        /// Check if role exists
        /// </summary>
        bool RoleExists(string roleName);

        /// <summary>
        /// Create new role
        /// </summary>
        bool CreateRole(string roleName, string? description = null);

        /// <summary>
        /// Update existing role
        /// </summary>
        bool UpdateRole(int roleId, string roleName, string? description = null);

        /// <summary>
        /// Delete role
        /// </summary>
        bool DeleteRole(int roleId);

        // ==================== USER-ROLE MANAGEMENT ====================

        /// <summary>
        /// Get all users in a specific role
        /// </summary>
        List<ApplicationUser> GetUsersInRole(string roleName);

        /// <summary>
        /// Get all users NOT in a specific role
        /// </summary>
        List<ApplicationUser> GetUsersNotInRole(string roleName);

        /// <summary>
        /// Add user to role
        /// </summary>
        bool AddUserToRole(int userId, string roleName);

        /// <summary>
        /// Remove user from role
        /// </summary>
        bool RemoveUserFromRole(int userId, string roleName);

        /// <summary>
        /// Get user's current role
        /// </summary>
        string GetUserRole(int userId);
    }
}