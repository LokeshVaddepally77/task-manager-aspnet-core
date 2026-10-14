using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.ViewModels
{
    public class TaskHistoryViewModel
    {
        public TaskItem? Task { get; set; }
        public List<TaskHistory> ActivityTimeline { get; set; } = new List<TaskHistory>();
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public int TotalActivities { get; set; } = 0;
        public int TotalComments { get; set; } = 0;
        public int TotalAttachments { get; set; } = 0;
    }
}