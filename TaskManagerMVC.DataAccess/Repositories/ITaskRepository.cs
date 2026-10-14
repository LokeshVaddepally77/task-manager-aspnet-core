using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.Repository
{
    /// <summary>
    /// Task Repository Interface
    /// Defines all data operations for the Task Manager
    /// </summary>
    public interface ITaskRepository
    {
        // ==================== TASK OPERATIONS ====================

        List<TaskItem> GetAllTasks();
        TaskItem? GetTaskById(int id);
        List<TaskItem> GetTasksByAssignee(int userId);
        List<TaskItem> GetTasksByCreator(int userId);
        List<TaskItem> GetTasksByStatus(string status);
        List<TaskItem> GetTasksByPriority(string priority);
        List<TaskItem> GetTasksByCategory(int categoryId);
        List<TaskItem> GetOverdueTasks();
        List<TaskItem> GetOverdueTasksForUser(int userId);
        List<TaskItem> GetCompletedTasks();
        List<TaskItem> GetTasksInProgress();
        List<TaskItem> SearchTasks(string searchTerm);

        TaskItem CreateTask(TaskItem taskItem);
        TaskItem UpdateTask(TaskItem taskItem);
        bool DeleteTask(int id);

        void UpdateTaskStatus(int taskId, string newStatus);
        void ReassignTask(int taskId, int newAssigneeId);
        void UpdateCompletionPercentage(int taskId, int percentage);

        // ==================== USER OPERATIONS ====================

        List<ApplicationUser> GetAllUsers();
        List<ApplicationUser> GetActiveUsers();
        ApplicationUser? GetUserById(int id);
        ApplicationUser? GetUserByUsername(string username);

        // ==================== CATEGORY OPERATIONS ====================

        List<Category> GetAllCategories();
        Category? GetCategoryById(int id);

        // ==================== TAG OPERATIONS ====================

        List<Tag> GetAllTags();
        void AddTagToTask(int taskId, int tagId);
        void RemoveTagFromTask(int taskId, int tagId);

        // ==================== COMMENT OPERATIONS ====================

        List<Comment> GetTaskComments(int taskId);
        void AddTaskComment(int taskId, int userId, string content);
        void DeleteComment(int commentId);

        // ==================== ATTACHMENT OPERATIONS ====================

        List<Attachment> GetTaskAttachments(int taskId);
        void AddTaskAttachment(int taskId, string fileName, string changeType);
        void DeleteTaskAttachment(int attachmentId);

        // ==================== HISTORY OPERATIONS ====================

        List<TaskHistory> GetTaskHistory(int taskId);
        void LogTaskChange(int taskId, string fieldName, string oldValue, string newValue, int changedByUserId);

        // ==================== NOTIFICATION OPERATIONS ====================

        List<Notification> GetUserNotifications(int userId);
        List<Notification> GetUnreadNotifications(int userId);
        void MarkNotificationAsRead(int notificationId);
        void CreateNotification(int taskId, int userId, string message);

        // ==================== STATISTICS ====================

        Dictionary<string, int> GetUserStatistics(int userId);
        Dictionary<string, object> GetTeamStatistics();
    }
}