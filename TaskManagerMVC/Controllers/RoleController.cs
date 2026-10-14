using Microsoft.AspNetCore.Mvc;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.Services;
using TaskManagerMVC.DataAccess.ViewModels;
using TaskManagerMVC.Web.Filters;

namespace TaskManagerMVC.Web.Controllers
{
    /// <summary>
    /// Role Management Controller
    /// Implements ASP.NET Core role-based authorization management
    /// </summary>
    [Authenticate]
    [ManagerOnly]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;
        private readonly IAuthenticationService _authService;

        public RoleController(IRoleService roleService, IAuthenticationService authService)
        {
            _roleService = roleService;
            _authService = authService;
        }

        // ==================== LIST ALL ROLES ====================

        /// <summary>
        /// Get list of all roles
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Role Management";
            var roles = _roleService.GetAllRoles();
            return View(roles);
        }

        // ==================== CREATE ROLE ====================

        /// <summary>
        /// Create new role - GET
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Role";
            return View(new RoleViewModel());
        }

        /// <summary>
        /// Create new role - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_roleService.RoleExists(model.RoleName))
            {
                ModelState.AddModelError("RoleName", "Role already exists.");
                return View(model);
            }

            var result = _roleService.CreateRole(model.RoleName, model.Description);
            if (result)
            {
                TempData["Success"] = $"Role '{model.RoleName}' created successfully!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to create role.");
            return View(model);
        }

        // ==================== EDIT ROLE ====================

        /// <summary>
        /// Edit role - GET
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewData["Title"] = "Edit Role";
            var role = _roleService.GetRoleById(id);

            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            var model = new RoleViewModel
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                UserCount = _roleService.GetUsersInRole(role.RoleName).Count
            };

            return View(model);
        }

        /// <summary>
        /// Edit role - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = _roleService.UpdateRole(model.RoleId, model.RoleName, model.Description);
            if (result)
            {
                TempData["Success"] = $"Role '{model.RoleName}' updated successfully!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to update role.");
            return View(model);
        }

        // ==================== DELETE ROLE ====================

        /// <summary>
        /// Delete role - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var role = _roleService.GetRoleById(id);

            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            // Prevent deleting roles with users
            var usersInRole = _roleService.GetUsersInRole(role.RoleName);
            if (usersInRole.Any())
            {
                TempData["Error"] = $"Cannot delete role '{role.RoleName}'. It has {usersInRole.Count} user(s) assigned.";
                return RedirectToAction("Index");
            }

            // Prevent deleting default roles
            if (role.RoleName == "Manager" || role.RoleName == "Employee")
            {
                TempData["Error"] = "Cannot delete default system roles.";
                return RedirectToAction("Index");
            }

            var result = _roleService.DeleteRole(id);
            if (result)
            {
                TempData["Success"] = $"Role '{role.RoleName}' deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete role.";
            }

            return RedirectToAction("Index");
        }

        // ==================== MANAGE USERS IN ROLE ====================

        /// <summary>
        /// View users in a specific role
        /// </summary>
        [HttpGet]
        public IActionResult UsersInRole(int id)
        {
            var role = _roleService.GetRoleById(id);

            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            ViewData["Title"] = $"Users in '{role.RoleName}' Role";

            var model = new RoleUsersViewModel
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                UsersInRole = _roleService.GetUsersInRole(role.RoleName),
                UsersNotInRole = _roleService.GetUsersNotInRole(role.RoleName)
            };

            return View(model);
        }

        /// <summary>
        /// Add user to role - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUserToRole(int roleId, int userId)
        {
            var role = _roleService.GetRoleById(roleId);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            var result = _roleService.AddUserToRole(userId, role.RoleName);
            if (result)
            {
                TempData["Success"] = "User added to role successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to add user to role.";
            }

            return RedirectToAction("UsersInRole", new { id = roleId });
        }

        /// <summary>
        /// Remove user from role - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveUserFromRole(int roleId, int userId)
        {
            var role = _roleService.GetRoleById(roleId);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            // Prevent removing the last manager
            if (role.RoleName == "Manager")
            {
                var managersCount = _roleService.GetUsersInRole("Manager").Count;
                if (managersCount <= 1)
                {
                    TempData["Error"] = "Cannot remove the last manager from the system.";
                    return RedirectToAction("UsersInRole", new { id = roleId });
                }
            }

            var result = _roleService.RemoveUserFromRole(userId, role.RoleName);
            if (result)
            {
                TempData["Success"] = "User removed from role successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to remove user from role.";
            }

            return RedirectToAction("UsersInRole", new { id = roleId });
        }
    }
}