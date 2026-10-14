using Microsoft.AspNetCore.Mvc;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.Repository;
using TaskManagerMVC.DataAccess.ViewModels;
using TaskManagerMVC.Models;
using TaskManagerMVC.Web.Filters;

namespace TaskManagerMVC.Web.Controllers
{
    [Authenticate]
    [ManagerOnly]
    public class TeamController : Controller
    {
        private readonly ITaskRepository _taskRepository;

        public TeamController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private string CurrentUsername => HttpContext.Session.GetString("Username") ?? "";
        private bool IsManager => HttpContext.Session.GetString("Role") == "Manager";

        // ==================== INDEX ====================

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Team";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var teamMembers = _taskRepository.GetActiveUsers();
            var allTasks = _taskRepository.GetAllTasks();
            var completedTasks = _taskRepository.GetCompletedTasks();

            var teamVM = new TeamViewModel
            {
                TeamMembers = teamMembers,
                TotalTeamTasks = allTasks.Count,
                TotalCompletedTasks = completedTasks.Count,
                TeamCompletionRate = allTasks.Count > 0
                    ? (completedTasks.Count * 100.0) / allTasks.Count
                    : 0,
                TeamOverdueTasks = _taskRepository.GetOverdueTasks().Count
            };

            return View(teamVM);
        }

        // ==================== MEMBER DETAILS ====================

        [HttpGet]
        public IActionResult MemberDetails(int userId)
        {
            ViewData["Title"] = "Member Details";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var user = _taskRepository.GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var teamVM = new TeamViewModel
            {
                TeamMembers = new List<ApplicationUser> { user }
            };

            return View(teamVM);
        }

        // ==================== MEMBER TASKS ====================

        [HttpGet]
        public IActionResult MemberTasks(int userId)
        {
            var user = _taskRepository.GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"{user.FullName}'s Tasks";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;
            ViewBag.MemberName = user.FullName;
            ViewBag.MemberId = userId;

            var tasks = _taskRepository.GetTasksByAssignee(userId);
            return View(tasks);
        }

        // ==================== REASSIGN TASK ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReassignTask(int taskId, int newAssigneeId)
        {
            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("Index");
            }

            var newAssignee = _taskRepository.GetUserById(newAssigneeId);
            if (newAssignee == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            _taskRepository.ReassignTask(taskId, newAssigneeId);
            TempData["Success"] = $"Task '{task.Title}' reassigned to {newAssignee.FullName}";

            return RedirectToAction("Index");
        }

        // ==================== TEAM STATISTICS ====================

        [HttpGet]
        public IActionResult Statistics()
        {
            ViewData["Title"] = "Team Statistics";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var stats = _taskRepository.GetTeamStatistics();
            return View(stats);
        }
    }
}