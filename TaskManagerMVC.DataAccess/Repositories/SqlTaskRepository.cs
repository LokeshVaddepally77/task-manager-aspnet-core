using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Data;
using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.Repository
{
    public class SQLTaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public SQLTaskRepository(AppDbContext context)
        {
            _context = context;
        }

        // ==================== TASK OPERATIONS ====================

        public List<TaskItem> GetAllTasks()
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.CategoryNavigation)
                .Include(t => t.Comments)
                .Include(t => t.Attachments)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();
        }

        public TaskItem? GetTaskById(int id)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.CategoryNavigation)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .FirstOrDefault(t => t.TaskId == id);
        }

        public List<TaskItem> GetTasksByAssignee(int userId)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Include(t => t.Comments)
                .Where(t => t.AssignedToId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();
        }

        public List<TaskItem> GetTasksByCreator(int userId)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.CreatedById == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();
        }

        public List<TaskItem> GetTasksByStatus(string status)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.Status == status)
                .ToList();
        }

        public List<TaskItem> GetTasksByPriority(string priority)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.Priority == priority)
                .ToList();
        }

        public List<TaskItem> GetTasksByCategory(int categoryId)
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.CategoryId == categoryId)
                .ToList();
        }

        public List<TaskItem> GetOverdueTasks()
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.DueDate < DateTime.Now && t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToList();
        }

        public List<TaskItem> GetOverdueTasksForUser(int userId)
        {
            return _context.Tasks
                .Include(t => t.CategoryNavigation)
                .Where(t => t.AssignedToId == userId &&
                           t.DueDate < DateTime.Now &&
                           t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToList();
        }

        public List<TaskItem> GetCompletedTasks()
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.Status == "Completed")
                .OrderByDescending(t => t.LastModifiedDate)
                .ToList();
        }

        public List<TaskItem> GetTasksInProgress()
        {
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.Status == "In Progress")
                .ToList();
        }

        public List<TaskItem> SearchTasks(string searchTerm)
        {
            var lowerSearch = searchTerm.ToLower();
            return _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.Title.ToLower().Contains(lowerSearch) ||
                           t.Description.ToLower().Contains(lowerSearch))
                .ToList();
        }

        public TaskItem CreateTask(TaskItem taskItem)
        {
            taskItem.CreatedDate = DateTime.Now;
            taskItem.LastModifiedDate = DateTime.Now;

            _context.Tasks.Add(taskItem);
            _context.SaveChanges();

            // Log creation
            LogTaskChange(taskItem.TaskId, "Task", "", "Created", taskItem.CreatedById);

            // Create notification for assignee
            if (taskItem.AssignedToId != taskItem.CreatedById)
            {
                CreateNotification(taskItem.TaskId, taskItem.AssignedToId,
                    $"Task '{taskItem.Title}' has been assigned to you");
            }

            return taskItem;
        }

        public TaskItem UpdateTask(TaskItem taskItem)
        {
            var existingTask = GetTaskById(taskItem.TaskId);
            if (existingTask != null)
            {
                // Log changes
                if (existingTask.Title != taskItem.Title)
                    LogTaskChange(taskItem.TaskId, "Title", existingTask.Title, taskItem.Title, taskItem.CreatedById);
                if (existingTask.Status != taskItem.Status)
                    LogTaskChange(taskItem.TaskId, "Status", existingTask.Status, taskItem.Status, taskItem.CreatedById);
                if (existingTask.Priority != taskItem.Priority)
                    LogTaskChange(taskItem.TaskId, "Priority", existingTask.Priority, taskItem.Priority, taskItem.CreatedById);
                if (existingTask.AssignedToId != taskItem.AssignedToId)
                {
                    var oldUser = GetUserById(existingTask.AssignedToId);
                    var newUser = GetUserById(taskItem.AssignedToId);
                    LogTaskChange(taskItem.TaskId, "AssignedTo",
                        oldUser?.Username ?? "Unknown",
                        newUser?.Username ?? "Unknown",
                        taskItem.CreatedById);
                }

                taskItem.LastModifiedDate = DateTime.Now;
                _context.Entry(existingTask).CurrentValues.SetValues(taskItem);
                _context.SaveChanges();
            }
            return taskItem;
        }

        public bool DeleteTask(int id)
        {
            var task = _context.Tasks.Find(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public void UpdateTaskStatus(int taskId, string newStatus)
        {
            var task = _context.Tasks.Find(taskId);
            if (task != null)
            {
                var oldStatus = task.Status;
                task.Status = newStatus;
                task.LastModifiedDate = DateTime.Now;

                if (newStatus == "Completed")
                    task.CompletionPercentage = 100;

                _context.SaveChanges();
                LogTaskChange(taskId, "Status", oldStatus, newStatus, task.AssignedToId);
            }
        }

        public void ReassignTask(int taskId, int newAssigneeId)
        {
            var task = _context.Tasks.Find(taskId);
            if (task != null)
            {
                var oldAssignee = GetUserById(task.AssignedToId);
                task.AssignedToId = newAssigneeId;
                task.LastModifiedDate = DateTime.Now;
                _context.SaveChanges();

                var newAssignee = GetUserById(newAssigneeId);
                LogTaskChange(taskId, "AssignedTo",
                    oldAssignee?.Username ?? "Unknown",
                    newAssignee?.Username ?? "Unknown",
                    task.CreatedById);

                CreateNotification(taskId, newAssigneeId,
                    $"Task '{task.Title}' has been assigned to you");
            }
        }

        public void UpdateCompletionPercentage(int taskId, int percentage)
        {
            var task = _context.Tasks.Find(taskId);
            if (task != null)
            {
                task.CompletionPercentage = percentage;
                task.LastModifiedDate = DateTime.Now;

                if (percentage >= 100)
                    task.Status = "Completed";

                _context.SaveChanges();
            }
        }

        // ==================== USER OPERATIONS ====================

        public List<ApplicationUser> GetAllUsers()
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .OrderBy(u => u.Username)
                .ToList();
        }

        public List<ApplicationUser> GetActiveUsers()
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .Where(u => u.IsActive)
                .OrderBy(u => u.UserName)
                .ToList();
        }

        public ApplicationUser? GetUserById(int id)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .FirstOrDefault(u => u.Id == id);
        }

        public ApplicationUser? GetUserByUsername(string username)
        {
            return _context.Users
                .Include(u => u.AssignedTasks)
                .FirstOrDefault(u => u.Username == username);
        }

        // ==================== CATEGORY OPERATIONS ====================

        public List<Category> GetAllCategories()
        {
            return _context.Categories.OrderBy(c => c.Name).ToList();
        }

        public Category? GetCategoryById(int id)
        {
            return _context.Categories.Find(id);
        }

        // ==================== TAG OPERATIONS ====================

        public List<Tag> GetAllTags()
        {
            return _context.Tags.OrderBy(t => t.Name).ToList();
        }

        public void AddTagToTask(int taskId, int tagId)
        {
            if (!_context.TaskTags.Any(tt => tt.TaskId == taskId && tt.TagId == tagId))
            {
                _context.TaskTags.Add(new TaskTag { TaskId = taskId, TagId = tagId });
                _context.SaveChanges();
            }
        }

        public void RemoveTagFromTask(int taskId, int tagId)
        {
            var taskTag = _context.TaskTags
                .FirstOrDefault(tt => tt.TaskId == taskId && tt.TagId == tagId);
            if (taskTag != null)
            {
                _context.TaskTags.Remove(taskTag);
                _context.SaveChanges();
            }
        }

        // ==================== COMMENT OPERATIONS ====================

        public List<Comment> GetTaskComments(int taskId)
        {
            return _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
        }

        public void AddTaskComment(int taskId, int userId, string content)
        {
            var comment = new Comment
            {
                TaskId = taskId,
                UserId = userId,
                Content = content,
                CreatedDate = DateTime.Now
            };
            _context.Comments.Add(comment);
            _context.SaveChanges();
        }
        public void DeleteComment(int commentId)
        {
            var comment = _context.Comments.Find(commentId);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
            }
        }

        // ==================== ATTACHMENT OPERATIONS ====================

        public List<Attachment> GetTaskAttachments(int taskId)
        {
            return _context.Attachments
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.UploadedDate)
                .ToList();
        }

        public void AddTaskAttachment(int taskId, string fileName, string changeType)
        {
            var attachment = new Attachment
            {
                TaskId = taskId,
                FileName = fileName,
                ChangeType = changeType,
                UploadedDate = DateTime.Now
            };
            _context.Attachments.Add(attachment);
            _context.SaveChanges();
        }

        // ==================== HISTORY OPERATIONS ====================

        public List<TaskHistory> GetTaskHistory(int taskId)
        {
            return _context.TaskHistories
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.ChangedDate)
                .ToList();
        }

        public void LogTaskChange(int taskId, string fieldName, string oldValue, string newValue, int changedByUserId)
        {
            var user = GetUserById(changedByUserId);
            var history = new TaskHistory
            {
                TaskId = taskId,
                FieldName = fieldName,
                OldValue = oldValue ?? "N/A",
                NewValue = newValue ?? "N/A",
                ChangedBy = user?.Username ?? "System",
                ChangedDate = DateTime.Now
            };
            _context.TaskHistories.Add(history);
            _context.SaveChanges();
        }

        // ==================== NOTIFICATION OPERATIONS ====================

        public List<Notification> GetUserNotifications(int userId)
        {
            return _context.Notifications
                .Include(n => n.Task)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();
        }

        public List<Notification> GetUnreadNotifications(int userId)
        {
            return _context.Notifications
                .Include(n => n.Task)
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();
        }

        public void MarkNotificationAsRead(int notificationId)
        {
            var notification = _context.Notifications.Find(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }
        }

        public void CreateNotification(int taskId, int userId, string message)
        {
            var notification = new Notification
            {
                TaskId = taskId,
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        // ==================== STATISTICS ====================

        public Dictionary<string, int> GetUserStatistics(int userId)
        {
            var userTasks = GetTasksByAssignee(userId);
            return new Dictionary<string, int>
            {
                { "Total", userTasks.Count },
                { "Completed", userTasks.Count(t => t.Status == "Completed") },
                { "InProgress", userTasks.Count(t => t.Status == "In Progress") },
                { "New", userTasks.Count(t => t.Status == "New") },
                { "Overdue", userTasks.Count(t => t.DueDate < DateTime.Now && t.Status != "Completed") }
            };
        }

        public Dictionary<string, object> GetTeamStatistics()
        {
            var allTasks = GetAllTasks();
            var completedCount = allTasks.Count(t => t.Status == "Completed");

            return new Dictionary<string, object>
            {
                { "TotalTasks", allTasks.Count },
                { "CompletedTasks", completedCount },
                { "InProgressTasks", allTasks.Count(t => t.Status == "In Progress") },
                { "OverdueTasks", GetOverdueTasks().Count },
                { "TeamMembers", GetActiveUsers().Count },
                { "CompletionRate", allTasks.Count > 0 ? (completedCount * 100.0) / allTasks.Count : 0 }
            };
        }
        public void DeleteTaskAttachment(int attachmentId)
        {
            var attachment = _context.Attachments.Find(attachmentId);
            if (attachment != null)
            {
                _context.Attachments.Remove(attachment);
                _context.SaveChanges();
            }
        }
    }
}