using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.ViewModels
{
    public class DashboardViewModel
    {
        public ApplicationUser? CurrentUser { get; set; }
        public List<TaskItem> MyTasks { get; set; } = new List<TaskItem>();
        public List<TaskItem> HighPriorityTasks { get; set; } = new List<TaskItem>();
        public List<TaskItem> OverdueTasks { get; set; } = new List<TaskItem>();
        public int TasksCompletedToday { get; set; } = 0;
        public int TasksInProgress { get; set; } = 0;
        public int TasksNotStarted { get; set; } = 0;
        public List<string> RecentNotifications { get; set; } = new List<string>();
        public int TeamMembersCount { get; set; } = 0;
        public string WelcomeMessage { get; set; } = "Good Morning!";
        public DateTime CurrentDateTime { get; set; } = DateTime.Now;

        // Computed properties
        public int TotalTasks => MyTasks?.Count ?? 0;
        public int CompletedTasks => MyTasks?.Count(t => t.Status == "Completed") ?? 0;
        public double CompletionRate => TotalTasks > 0 ? (CompletedTasks * 100.0) / TotalTasks : 0;
    }
}