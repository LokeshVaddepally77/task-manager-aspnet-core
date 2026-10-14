using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.ViewModels
{
    public class TeamViewModel
    {
        public List<ApplicationUser> TeamMembers { get; set; } = new List<ApplicationUser>();
        public int TotalTeamTasks { get; set; } = 0;
        public int TotalCompletedTasks { get; set; } = 0;
        public double TeamCompletionRate { get; set; } = 0;
        public int TeamOverdueTasks { get; set; } = 0;

        // Get task count for a user by UserId
        public int GetUserTaskCount(int userId)
        {
            return TeamMembers.FirstOrDefault(m => m.UserId == userId)?
                .AssignedTasks?.Count ?? 0;
        }

        // Overload for username (backward compatibility with views)
        public int GetUserTaskCount(string username)
        {
            return TeamMembers.FirstOrDefault(m => m.Username == username)?
                .AssignedTasks?.Count ?? 0;
        }

        // Get completed task count for a user
        public int GetUserCompletedTasks(int userId)
        {
            return TeamMembers.FirstOrDefault(m => m.UserId == userId)?
                .AssignedTasks?.Count(t => t.Status == "Completed") ?? 0;
        }

        public int GetUserCompletedTasks(string username)
        {
            return TeamMembers.FirstOrDefault(m => m.Username == username)?
                .AssignedTasks?.Count(t => t.Status == "Completed") ?? 0;
        }

        // Get completion rate for a user
        public double GetUserCompletionRate(int userId)
        {
            var user = TeamMembers.FirstOrDefault(m => m.UserId == userId);
            if (user?.AssignedTasks == null || user.AssignedTasks.Count == 0) return 0;
            return (user.AssignedTasks.Count(t => t.Status == "Completed") * 100.0) / user.AssignedTasks.Count;
        }

        public double GetUserCompletionRate(string username)
        {
            var user = TeamMembers.FirstOrDefault(m => m.Username == username);
            if (user?.AssignedTasks == null || user.AssignedTasks.Count == 0) return 0;
            return (user.AssignedTasks.Count(t => t.Status == "Completed") * 100.0) / user.AssignedTasks.Count;
        }

        // Get overdue tasks count for a user
        public int GetUserOverdueTasks(int userId)
        {
            return TeamMembers.FirstOrDefault(m => m.UserId == userId)?
                .AssignedTasks?.Count(t => t.DueDate < DateTime.Now && t.Status != "Completed") ?? 0;
        }

        public int GetUserOverdueTasks(string username)
        {
            return TeamMembers.FirstOrDefault(m => m.Username == username)?
                .AssignedTasks?.Count(t => t.DueDate < DateTime.Now && t.Status != "Completed") ?? 0;
        }

        // Get in-progress tasks count for a user
        public int GetUserInProgressTasks(int userId)
        {
            return TeamMembers.FirstOrDefault(m => m.UserId == userId)?
                .AssignedTasks?.Count(t => t.Status == "In Progress") ?? 0;
        }

        public int GetUserInProgressTasks(string username)
        {
            return TeamMembers.FirstOrDefault(m => m.Username == username)?
                .AssignedTasks?.Count(t => t.Status == "In Progress") ?? 0;
        }
    }
}