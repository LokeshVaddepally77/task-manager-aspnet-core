using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Data;
using TaskManagerMVC.DataAccess.Models;
using TaskManagerMVC.DataAccess.Repository;

namespace TaskManagerMVC.Tests
{
    /// <summary>
    /// Runs the SQL repository against EF Core's in-memory provider.
    /// Each test gets its own empty database with a manager and two employees.
    /// </summary>
    public class TaskRepositoryTests : IDisposable
    {
        private const int ManagerId = 1;
        private const int AliceId = 2;
        private const int BobId = 3;

        private readonly AppDbContext _context;
        private readonly SQLTaskRepository _repository;

        public TaskRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _context.Users.AddRange(
                new ApplicationUser { Id = ManagerId, UserName = "manager", Role = "Manager" },
                new ApplicationUser { Id = AliceId, UserName = "alice" },
                new ApplicationUser { Id = BobId, UserName = "bob", IsActive = false });
            _context.SaveChanges();
            _repository = new SQLTaskRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        private TaskItem NewTask(string title = "Prepare sprint demo", int assignedTo = AliceId,
            string status = "New", int dueInDays = 7) => new()
        {
            Title = title,
            Description = "Build the slides and a short walkthrough.",
            Priority = "Medium",
            Status = status,
            DueDate = DateTime.Now.AddDays(dueInDays),
            CreatedById = ManagerId,
            AssignedToId = assignedTo
        };

        // ==================== CREATE ====================

        [Fact]
        public void CreateTask_RecordsCreationInHistory()
        {
            var task = _repository.CreateTask(NewTask());

            var history = Assert.Single(_repository.GetTaskHistory(task.TaskId));
            Assert.Equal("Task", history.FieldName);
            Assert.Equal("Created", history.NewValue);
            Assert.Equal("manager", history.ChangedBy);
        }

        [Fact]
        public void CreateTask_AssignedToSomeoneElse_NotifiesAssignee()
        {
            var task = _repository.CreateTask(NewTask("Fix login bug"));

            var notification = Assert.Single(_repository.GetUnreadNotifications(AliceId));
            Assert.Equal(task.TaskId, notification.TaskId);
            Assert.Contains("Fix login bug", notification.Message);
        }

        [Fact]
        public void CreateTask_AssignedToCreator_SendsNoNotification()
        {
            _repository.CreateTask(NewTask(assignedTo: ManagerId));

            Assert.Empty(_repository.GetUserNotifications(ManagerId));
        }

        // ==================== UPDATE AND AUDIT TRAIL ====================

        [Fact]
        public void UpdateTask_LogsOnlyTheFieldsThatChanged()
        {
            var task = _repository.CreateTask(NewTask("Old title"));
            var edited = NewTask("New title");
            edited.TaskId = task.TaskId;
            edited.Priority = "High";

            _repository.UpdateTask(edited);

            var changes = _repository.GetTaskHistory(task.TaskId)
                .Where(h => h.FieldName != "Task")
                .ToDictionary(h => h.FieldName);
            Assert.Equal(2, changes.Count);
            Assert.Equal("Old title", changes["Title"].OldValue);
            Assert.Equal("New title", changes["Title"].NewValue);
            Assert.Equal("Medium", changes["Priority"].OldValue);
            Assert.Equal("High", changes["Priority"].NewValue);
            Assert.Equal("New title", _repository.GetTaskById(task.TaskId)!.Title);
        }

        [Fact]
        public void UpdateTaskStatus_ToCompleted_SetsCompletionTo100AndLogsChange()
        {
            var task = _repository.CreateTask(NewTask(status: "In Progress"));

            _repository.UpdateTaskStatus(task.TaskId, "Completed");

            var saved = _repository.GetTaskById(task.TaskId)!;
            Assert.Equal("Completed", saved.Status);
            Assert.Equal(100, saved.CompletionPercentage);
            Assert.Contains(_repository.GetTaskHistory(task.TaskId),
                h => h.FieldName == "Status" && h.OldValue == "In Progress" && h.NewValue == "Completed");
        }

        [Fact]
        public void UpdateCompletionPercentage_Reaching100_MarksTaskCompleted()
        {
            var task = _repository.CreateTask(NewTask(status: "In Progress"));

            _repository.UpdateCompletionPercentage(task.TaskId, 60);
            Assert.Equal("In Progress", _repository.GetTaskById(task.TaskId)!.Status);

            _repository.UpdateCompletionPercentage(task.TaskId, 100);
            Assert.Equal("Completed", _repository.GetTaskById(task.TaskId)!.Status);
        }

        [Fact]
        public void ReassignTask_LogsUsernamesAndNotifiesNewAssignee()
        {
            var task = _repository.CreateTask(NewTask(assignedTo: AliceId));

            _repository.ReassignTask(task.TaskId, BobId);

            Assert.Equal(BobId, _repository.GetTaskById(task.TaskId)!.AssignedToId);
            Assert.Contains(_repository.GetTaskHistory(task.TaskId),
                h => h.FieldName == "AssignedTo" && h.OldValue == "alice" && h.NewValue == "bob");
            Assert.Single(_repository.GetUnreadNotifications(BobId));
        }

        // ==================== QUERIES ====================

        [Fact]
        public void GetOverdueTasks_ReturnsPastDueTasksThatAreNotCompleted()
        {
            var overdue = _repository.CreateTask(NewTask("Overdue", dueInDays: -2));
            _repository.CreateTask(NewTask("Done late", status: "Completed", dueInDays: -5));
            _repository.CreateTask(NewTask("Future", dueInDays: 3));

            var result = Assert.Single(_repository.GetOverdueTasks());
            Assert.Equal(overdue.TaskId, result.TaskId);
            Assert.True(result.IsOverdue);
        }

        [Fact]
        public void SearchTasks_MatchesTitleOrDescription_IgnoringCase()
        {
            _repository.CreateTask(NewTask("Deploy to AZURE"));
            _repository.CreateTask(NewTask("Write unit tests"));

            Assert.Single(_repository.SearchTasks("azure"));
            Assert.Equal(2, _repository.SearchTasks("WALKTHROUGH").Count);
            Assert.Empty(_repository.SearchTasks("kubernetes"));
        }

        [Fact]
        public void GetTeamStatistics_CountsTasksMembersAndCompletionRate()
        {
            _repository.CreateTask(NewTask("One", status: "Completed"));
            _repository.CreateTask(NewTask("Two", status: "In Progress"));
            _repository.CreateTask(NewTask("Three", status: "New", dueInDays: -1));
            _repository.CreateTask(NewTask("Four", status: "Completed"));

            var stats = _repository.GetTeamStatistics();

            Assert.Equal(4, stats["TotalTasks"]);
            Assert.Equal(2, stats["CompletedTasks"]);
            Assert.Equal(1, stats["InProgressTasks"]);
            Assert.Equal(1, stats["OverdueTasks"]);
            Assert.Equal(2, stats["TeamMembers"]); // bob is inactive
            Assert.Equal(50.0, stats["CompletionRate"]);
        }

        [Fact]
        public void DeleteTask_RemovesTask_AndReturnsFalseWhenMissing()
        {
            var task = _repository.CreateTask(NewTask());

            Assert.True(_repository.DeleteTask(task.TaskId));
            Assert.Null(_repository.GetTaskById(task.TaskId));
            Assert.False(_repository.DeleteTask(task.TaskId));
        }
    }
}
