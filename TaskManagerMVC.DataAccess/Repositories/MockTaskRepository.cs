using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.Repository
{
    /// <summary>
    /// Mock Repository for Testing (Optional - can be deleted if not needed)
    /// Uses in-memory data instead of database
    /// </summary>
    public class MockTaskRepository : ITaskRepository
    {
        private readonly List<ApplicationUser> _users;
        private readonly List<TaskItem> _tasks;
        private readonly List<Category> _categories;
        private readonly List<Tag> _tags;
        private readonly List<TaskTag> _taskTags;
        private readonly List<Comment> _comments;
        private readonly List<Notification> _notifications;
        private readonly List<Attachment> _attachments;
        private readonly List<TaskHistory> _taskHistories;

        public MockTaskRepository()
        {
            // Initialize Users
            _users = new List<ApplicationUser>
            {
                new ApplicationUser { UserId = 1, Username = "Sarah", Email = "sarah@company.com", Role = "Manager", FirstName = "Sarah", LastName = "Johnson", PasswordHash = "", IsActive = true },
                new ApplicationUser { UserId = 2, Username = "John", Email = "john@company.com", Role = "Employee", FirstName = "John", LastName = "Smith", PasswordHash = "", IsActive = true },
                new ApplicationUser { UserId = 3, Username = "Mike", Email = "mike@company.com", Role = "Employee", FirstName = "Mike", LastName = "Wilson", PasswordHash = "", IsActive = true },
                new ApplicationUser { UserId = 4, Username = "David", Email = "david@company.com", Role = "Manager", FirstName = "David", LastName = "Brown", PasswordHash = "", IsActive = true }
            };

            // Initialize Categories
            _categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Development", ColorCode = "#3B82F6" },
                new Category { CategoryId = 2, Name = "Bug Fix", ColorCode = "#EF4444" },
                new Category { CategoryId = 3, Name = "Design", ColorCode = "#8B5CF6" }
            };

            // Initialize Tags
            _tags = new List<Tag>
            {
                new Tag { TagId = 1, Name = "Urgent", ColorCode = "#DC2626" },
                new Tag { TagId = 2, Name = "Frontend", ColorCode = "#3B82F6" },
                new Tag { TagId = 3, Name = "Backend", ColorCode = "#10B981" }
            };

            // Initialize Tasks
            _tasks = new List<TaskItem>
            {
                new TaskItem { TaskId = 1, Title = "Sample Task 1", Description = "Description here", Priority = "High", Status = "In Progress", CreatedById = 1, AssignedToId = 1, CategoryId = 1, DueDate = DateTime.Now.AddDays(3), CompletionPercentage = 50 },
                new TaskItem { TaskId = 2, Title = "Sample Task 2", Description = "Another description", Priority = "Medium", Status = "New", CreatedById = 1, AssignedToId = 2, CategoryId = 2, DueDate = DateTime.Now.AddDays(5), CompletionPercentage = 0 }
            };

            _taskTags = new List<TaskTag>();
            _comments = new List<Comment>();
            _notifications = new List<Notification>();
            _attachments = new List<Attachment>();
            _taskHistories = new List<TaskHistory>();

            // Link navigation properties
            foreach (var task in _tasks)
            {
                task.AssignedToUser = _users.FirstOrDefault(u => u.UserId == task.AssignedToId);
                task.CreatedByUser = _users.FirstOrDefault(u => u.UserId == task.CreatedById);
                task.CategoryNavigation = _categories.FirstOrDefault(c => c.CategoryId == task.CategoryId);
            }
        }

        // Task Operations
        public List<TaskItem> GetAllTasks() => _tasks;
        public TaskItem? GetTaskById(int id) => _tasks.FirstOrDefault(t => t.TaskId == id);
        public List<TaskItem> GetTasksByAssignee(int userId) => _tasks.Where(t => t.AssignedToId == userId).ToList();
        public List<TaskItem> GetTasksByCreator(int userId) => _tasks.Where(t => t.CreatedById == userId).ToList();
        public List<TaskItem> GetTasksByStatus(string status) => _tasks.Where(t => t.Status == status).ToList();
        public List<TaskItem> GetTasksByPriority(string priority) => _tasks.Where(t => t.Priority == priority).ToList();
        public List<TaskItem> GetTasksByCategory(int categoryId) => _tasks.Where(t => t.CategoryId == categoryId).ToList();
        public List<TaskItem> GetOverdueTasks() => _tasks.Where(t => t.DueDate < DateTime.Now && t.Status != "Completed").ToList();
        public List<TaskItem> GetOverdueTasksForUser(int userId) => _tasks.Where(t => t.AssignedToId == userId && t.DueDate < DateTime.Now && t.Status != "Completed").ToList();
        public List<TaskItem> GetCompletedTasks() => _tasks.Where(t => t.Status == "Completed").ToList();
        public List<TaskItem> GetTasksInProgress() => _tasks.Where(t => t.Status == "In Progress").ToList();
        public List<TaskItem> SearchTasks(string searchTerm) => _tasks.Where(t => t.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        public TaskItem CreateTask(TaskItem taskItem)
        {
            taskItem.TaskId = _tasks.Max(t => t.TaskId) + 1;
            taskItem.CreatedDate = DateTime.Now;
            taskItem.LastModifiedDate = DateTime.Now;
            _tasks.Add(taskItem);
            return taskItem;
        }

        public TaskItem UpdateTask(TaskItem taskItem)
        {
            var existing = _tasks.FirstOrDefault(t => t.TaskId == taskItem.TaskId);
            if (existing != null)
            {
                var index = _tasks.IndexOf(existing);
                taskItem.LastModifiedDate = DateTime.Now;
                _tasks[index] = taskItem;
            }
            return taskItem;
        }

        public bool DeleteTask(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.TaskId == id);
            if (task != null)
            {
                _tasks.Remove(task);
                return true;
            }
            return false;
        }

        public void UpdateTaskStatus(int taskId, string newStatus)
        {
            var task = _tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null) task.Status = newStatus;
        }

        public void ReassignTask(int taskId, int newAssigneeId)
        {
            var task = _tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null) task.AssignedToId = newAssigneeId;
        }

        public void UpdateCompletionPercentage(int taskId, int percentage)
        {
            var task = _tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null) task.CompletionPercentage = percentage;
        }
        public void DeleteTaskAttachment(int attachmentId)
        {
            var attachment = _attachments.FirstOrDefault(a => a.AttachmentId == attachmentId);
            if (attachment != null)
            {
                _attachments.Remove(attachment);
            }
        }

        // User Operations
        public List<ApplicationUser> GetAllUsers() => _users;
        public List<ApplicationUser> GetActiveUsers() => _users.Where(u => u.IsActive).ToList();
        public ApplicationUser? GetUserById(int id) => _users.FirstOrDefault(u => u.UserId == id);
        public ApplicationUser? GetUserByUsername(string username) => _users.FirstOrDefault(u => u.Username == username);

        // Category Operations
        public List<Category> GetAllCategories() => _categories;
        public Category? GetCategoryById(int id) => _categories.FirstOrDefault(c => c.CategoryId == id);

        // Tag Operations
        public List<Tag> GetAllTags() => _tags;
        public void AddTagToTask(int taskId, int tagId) => _taskTags.Add(new TaskTag { TaskId = taskId, TagId = tagId });
        public void RemoveTagFromTask(int taskId, int tagId) => _taskTags.RemoveAll(tt => tt.TaskId == taskId && tt.TagId == tagId);

        // Comment Operations
        public List<Comment> GetTaskComments(int taskId) => _comments.Where(c => c.TaskId == taskId).ToList();
        public void AddTaskComment(int taskId, int userId, string content) => _comments.Add(new Comment { CommentId = _comments.Count + 1, TaskId = taskId, UserId = userId, Content = content, CreatedDate = DateTime.Now });
        public void DeleteComment(int commentId)
        {
            var comment = _comments.FirstOrDefault(c => c.CommentId == commentId);
            if (comment != null) _comments.Remove(comment);
        }
        // Attachment Operations
        public List<Attachment> GetTaskAttachments(int taskId) => _attachments.Where(a => a.TaskId == taskId).ToList();
        public void AddTaskAttachment(int taskId, string fileName, string changeType) => _attachments.Add(new Attachment { AttachmentId = _attachments.Count + 1, TaskId = taskId, FileName = fileName, ChangeType = changeType });

        // History Operations
        public List<TaskHistory> GetTaskHistory(int taskId) => _taskHistories.Where(h => h.TaskId == taskId).ToList();
        public void LogTaskChange(int taskId, string fieldName, string oldValue, string newValue, int changedByUserId) => _taskHistories.Add(new TaskHistory { HistoryId = _taskHistories.Count + 1, TaskId = taskId, FieldName = fieldName, OldValue = oldValue, NewValue = newValue, ChangedBy = GetUserById(changedByUserId)?.Username ?? "System", ChangedDate = DateTime.Now });

        // Notification Operations
        public List<Notification> GetUserNotifications(int userId) => _notifications.Where(n => n.UserId == userId).ToList();
        public List<Notification> GetUnreadNotifications(int userId) => _notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
        public void MarkNotificationAsRead(int notificationId) { var n = _notifications.FirstOrDefault(x => x.NotificationId == notificationId); if (n != null) n.IsRead = true; }
        public void CreateNotification(int taskId, int userId, string message) => _notifications.Add(new Notification { NotificationId = _notifications.Count + 1, TaskId = taskId, UserId = userId, Message = message });

        // Statistics
        public Dictionary<string, int> GetUserStatistics(int userId)
        {
            var tasks = GetTasksByAssignee(userId);
            return new Dictionary<string, int>
            {
                { "Total", tasks.Count },
                { "Completed", tasks.Count(t => t.Status == "Completed") },
                { "InProgress", tasks.Count(t => t.Status == "In Progress") },
                { "New", tasks.Count(t => t.Status == "New") },
                { "Overdue", tasks.Count(t => t.DueDate < DateTime.Now && t.Status != "Completed") }
            };
        }

        public Dictionary<string, object> GetTeamStatistics()
        {
            return new Dictionary<string, object>
            {
                { "TotalTasks", _tasks.Count },
                { "CompletedTasks", _tasks.Count(t => t.Status == "Completed") },
                { "InProgressTasks", _tasks.Count(t => t.Status == "In Progress") },
                { "OverdueTasks", GetOverdueTasks().Count },
                { "TeamMembers", _users.Count },
                { "CompletionRate", _tasks.Count > 0 ? (_tasks.Count(t => t.Status == "Completed") * 100.0) / _tasks.Count : 0 }
            };
        }
    }
}