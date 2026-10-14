using Microsoft.AspNetCore.Mvc;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.Repository;
using TaskManagerMVC.DataAccess.ViewModels;
using TaskManagerMVC.Models;
using TaskManagerMVC.Web.Filters;

namespace TaskManagerMVC.Web.Controllers
{
    [Authenticate]
    public class TaskController : Controller
    {
        private readonly ITaskRepository _taskRepository;

        public TaskController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        // Helper properties for current user
        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private string CurrentUsername => HttpContext.Session.GetString("Username") ?? "";
        private string CurrentRole => HttpContext.Session.GetString("Role") ?? "";
        private bool IsManager => CurrentRole == "Manager";

        // ==================== DASHBOARD ====================

        [HttpGet]
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var user = _taskRepository.GetUserById(CurrentUserId);

            // KEY CHANGE: Managers see ALL tasks, Employees see only their own
            List<TaskItem> tasksToDisplay;

            if (IsManager)
            {
                // Manager: Get ALL tasks in the system
                tasksToDisplay = _taskRepository.GetAllTasks();
            }
            else
            {
                // Employee: Get only tasks assigned to them
                tasksToDisplay = _taskRepository.GetTasksByAssignee(CurrentUserId);
            }

            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Good Morning" : hour < 17 ? "Good Afternoon" : "Good Evening";

            var dashboardVM = new DashboardViewModel
            {
                CurrentUser = user,
                MyTasks = tasksToDisplay,

                // High priority tasks that are not completed
                HighPriorityTasks = tasksToDisplay
                    .Where(t => t.Priority == "High" && t.Status != "Completed")
                    .ToList(),

                // Overdue tasks
                OverdueTasks = IsManager
                    ? _taskRepository.GetOverdueTasks()  // All overdue tasks for manager
                    : _taskRepository.GetOverdueTasksForUser(CurrentUserId),  // Only user's overdue

                // Status counts from the filtered task list
                TasksInProgress = tasksToDisplay.Count(t => t.Status == "In Progress"),
                TasksNotStarted = tasksToDisplay.Count(t => t.Status == "New"),
                TasksCompletedToday = tasksToDisplay.Count(t =>
                    t.Status == "Completed" &&
                    t.LastModifiedDate.Date == DateTime.Today),

                // Team members count (only relevant for managers)
                TeamMembersCount = IsManager ? _taskRepository.GetActiveUsers().Count : 0,

                WelcomeMessage = greeting,
                CurrentDateTime = DateTime.Now
            };

            return View(dashboardVM);
        }

        // ==================== INDEX (ALL TASKS) - MANAGER ONLY ====================

        [ManagerOnly]
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "All Tasks";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var tasks = _taskRepository.GetAllTasks();
            return View(tasks);
        }

        // ==================== MY TASKS ====================

        [HttpGet]
        public IActionResult MyTasks(string? status = null, string? priority = null)
        {
            ViewData["Title"] = "My Tasks";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var tasks = _taskRepository.GetTasksByAssignee(CurrentUserId);

            if (!string.IsNullOrEmpty(status))
            {
                tasks = tasks.Where(t => t.Status == status).ToList();
            }
            if (!string.IsNullOrEmpty(priority))
            {
                tasks = tasks.Where(t => t.Priority == priority).ToList();
            }

            return View(tasks);
        }

        // ==================== DETAILS ====================

        [HttpGet]
        public IActionResult Details(int id)
        {
            ViewData["Title"] = "Task Details";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }

            // Security: Employees can only view their own tasks
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(task);
        }

        // ==================== CREATE - MANAGER ONLY ====================

        [ManagerOnly]
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Task";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            ViewBag.Categories = _taskRepository.GetAllCategories();
            ViewBag.Users = _taskRepository.GetActiveUsers();
            ViewBag.Tags = _taskRepository.GetAllTags();

            var task = new TaskItem
            {
                CreatedById = CurrentUserId,
                DueDate = DateTime.Now.AddDays(7)
            };

            return View(task);
        }

        [ManagerOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromForm] TaskItem taskItem)
        {
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            // Set the creator
            taskItem.CreatedById = CurrentUserId;

            // Remove navigation property validation errors
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("CategoryNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    _taskRepository.CreateTask(taskItem);
                    TempData["Success"] = "Task created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating task: " + ex.Message);
                }
            }

            ViewBag.Categories = _taskRepository.GetAllCategories();
            ViewBag.Users = _taskRepository.GetActiveUsers();
            ViewBag.Tags = _taskRepository.GetAllTags();

            return View(taskItem);
        }

        // ==================== EDIT ====================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewData["Title"] = "Edit Task";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Categories = _taskRepository.GetAllCategories();
            ViewBag.Users = _taskRepository.GetActiveUsers();
            ViewBag.Tags = _taskRepository.GetAllTags();
            ViewBag.CanEditAll = IsManager;

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromRoute] int id, [FromForm] TaskItem taskItem)
        {
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            if (id != taskItem.TaskId)
            {
                return BadRequest();
            }

            var existingTask = _taskRepository.GetTaskById(id);
            if (existingTask == null)
            {
                return NotFound();
            }

            // Security check
            if (!IsManager && existingTask.AssignedToId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // If employee, only allow updating status and completion percentage
            if (!IsManager)
            {
                existingTask.Status = taskItem.Status;
                existingTask.CompletionPercentage = taskItem.CompletionPercentage;
                existingTask.LastModifiedDate = DateTime.Now;

                if (taskItem.CompletionPercentage >= 100)
                {
                    existingTask.Status = "Completed";
                }

                _taskRepository.UpdateTask(existingTask);
                TempData["Success"] = "Task progress updated successfully!";
                return RedirectToAction("MyTasks");
            }

            // Remove navigation property validation errors
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("CategoryNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    taskItem.CreatedById = existingTask.CreatedById;
                    taskItem.CreatedDate = existingTask.CreatedDate;
                    _taskRepository.UpdateTask(taskItem);
                    TempData["Success"] = "Task updated successfully!";
                    return RedirectToAction("Details", new { id = id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating task: " + ex.Message);
                }
            }

            ViewBag.Categories = _taskRepository.GetAllCategories();
            ViewBag.Users = _taskRepository.GetActiveUsers();
            ViewBag.Tags = _taskRepository.GetAllTags();
            ViewBag.CanEditAll = IsManager;

            return View(taskItem);
        }

        // ==================== DELETE - MANAGER ONLY ====================

        [ManagerOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var result = _taskRepository.DeleteTask(id);
            if (result)
            {
                TempData["Success"] = "Task deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete task.";
            }
            return RedirectToAction("Index");
        }

        // ==================== HISTORY ====================

        [HttpGet]
        public IActionResult History(int id)
        {
            ViewData["Title"] = "Task History";
            ViewData["CurrentUser"] = CurrentUsername;
            ViewData["IsManager"] = IsManager;

            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var historyVM = new TaskHistoryViewModel
            {
                Task = task,
                ActivityTimeline = _taskRepository.GetTaskHistory(id),
                Comments = _taskRepository.GetTaskComments(id),
                Attachments = _taskRepository.GetTaskAttachments(id),
                TotalActivities = _taskRepository.GetTaskHistory(id).Count,
                TotalComments = _taskRepository.GetTaskComments(id).Count,
                TotalAttachments = _taskRepository.GetTaskAttachments(id).Count
            };

            return View(historyVM);
        }

        // ==================== QUICK STATUS UPDATE ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status)
        {
            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("MyTasks");
            }

            _taskRepository.UpdateTaskStatus(id, status);
            TempData["Success"] = "Task status updated!";

            return RedirectToAction("MyTasks");
        }

        // ==================== QUICK PROGRESS UPDATE ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProgress(int id, int percentage)
        {
            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("MyTasks");
            }

            _taskRepository.UpdateCompletionPercentage(id, percentage);
            TempData["Success"] = "Progress updated!";

            return RedirectToAction("MyTasks");
        }

        // ==================== ADD COMMENT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int taskId, string content)
        {
            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("MyTasks");
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                _taskRepository.AddTaskComment(taskId, CurrentUserId, content);
                TempData["Success"] = "Comment added!";
            }

            return RedirectToAction("Details", new { id = taskId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(int commentId, int taskId)
        {
            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            var comment = task.Comments?.FirstOrDefault(c => c.CommentId == commentId);
            if (comment == null)
            {
                TempData["Error"] = "Comment not found.";
                return RedirectToAction("Details", new { id = taskId });
            }

            // Only managers or comment owner can delete
            if (!IsManager && comment.UserId != CurrentUserId)
            {
                TempData["Error"] = "You cannot delete this comment.";
                return RedirectToAction("Details", new { id = taskId });
            }

            _taskRepository.DeleteComment(commentId);
            TempData["Success"] = "Comment deleted!";

            return RedirectToAction("Details", new { id = taskId });
        }
        // ==================== ATTACHMENT OPERATIONS ====================

        // ==================== ATTACHMENT UPLOAD (MULTIPLE FILES) ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, List<IFormFile> files)
        {
            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("MyTasks");
            }

            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "Please select at least one file to upload.";
                return RedirectToAction("Details", new { id = taskId });
            }

            // Allowed extensions and max size
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".zip" };
            var maxFileSize = 5 * 1024 * 1024; // 5MB

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    failCount++;
                    errors.Add("Empty file skipped.");
                    continue;
                }

                // Validate file size
                if (file.Length > maxFileSize)
                {
                    failCount++;
                    errors.Add($"'{file.FileName}' exceeds 5MB limit.");
                    continue;
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    failCount++;
                    errors.Add($"'{file.FileName}' has invalid file type ({fileExtension}).");
                    continue;
                }

                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tasks", taskId.ToString());
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Save attachment record to database
                    _taskRepository.AddTaskAttachment(taskId, uniqueFileName, "Added");
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"Error uploading '{file.FileName}': {ex.Message}");
                }
            }

            // Set appropriate messages
            if (successCount > 0)
            {
                TempData["Success"] = successCount == 1
                    ? "File uploaded successfully!"
                    : $"{successCount} files uploaded successfully!";
            }

            if (failCount > 0)
            {
                TempData["Warning"] = string.Join(" | ", errors);
            }

            return RedirectToAction("Details", new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAttachment(int taskId, int attachmentId)
        {
            // SECURITY: Only managers can delete attachments
            if (!IsManager)
            {
                TempData["Error"] = "Only managers can delete attachments.";
                return RedirectToAction("Details", new { id = taskId });
            }

            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            try
            {
                var attachment = _taskRepository.GetTaskAttachments(taskId)
                    .FirstOrDefault(a => a.AttachmentId == attachmentId);

                if (attachment != null)
                {
                    // Delete file from disk
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tasks", taskId.ToString(), attachment.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Delete from database
                    _taskRepository.DeleteTaskAttachment(attachmentId);
                    TempData["Success"] = "Attachment deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting attachment: {ex.Message}";
            }

            return RedirectToAction("Details", new { id = taskId });
        }

        [HttpGet]
        public IActionResult DownloadAttachment(int taskId, int attachmentId)
        {
            var task = _taskRepository.GetTaskById(taskId);
            if (task == null)
            {
                return NotFound();
            }

            // Security check
            if (!IsManager && task.AssignedToId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var attachment = _taskRepository.GetTaskAttachments(taskId)
                .FirstOrDefault(a => a.AttachmentId == attachmentId);

            if (attachment == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tasks", taskId.ToString(), attachment.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("Details", new { id = taskId });
            }

            var contentType = GetContentType(attachment.FileName);
            var originalFileName = attachment.FileName.Contains("_")
                ? attachment.FileName.Substring(attachment.FileName.IndexOf("_") + 1)
                : attachment.FileName;

            return PhysicalFile(filePath, contentType, originalFileName);
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}