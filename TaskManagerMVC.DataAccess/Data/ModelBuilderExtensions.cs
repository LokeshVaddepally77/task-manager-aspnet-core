using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.Data
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            // Placeholder password - will be replaced on startup
            string placeholderHash = "PLACEHOLDER_WILL_BE_SET_ON_STARTUP";

            // ===== SEED USERS =====
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    UserId = 1,
                    Username = "Sarah",
                    Email = "sarah@company.com",
                    PasswordHash = placeholderHash,
                    Role = "Manager",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 1)
                },
                new ApplicationUser
                {
                    UserId = 2,
                    Username = "John",
                    Email = "john@company.com",
                    PasswordHash = placeholderHash,
                    Role = "Employee",
                    FirstName = "John",
                    LastName = "Smith",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 1, 15)
                },
                new ApplicationUser
                {
                    UserId = 3,
                    Username = "Mike",
                    Email = "mike@company.com",
                    PasswordHash = placeholderHash,
                    Role = "Employee",
                    FirstName = "Mike",
                    LastName = "Wilson",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 2, 1)
                },
                new ApplicationUser
                {
                    UserId = 4,
                    Username = "David",
                    Email = "david@company.com",
                    PasswordHash = placeholderHash,
                    Role = "Manager",
                    FirstName = "David",
                    LastName = "Brown",
                    IsActive = true,
                    CreatedDate = new DateTime(2025, 2, 15)
                }
            );

            // ===== SEED CATEGORIES =====
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Development", ColorCode = "#3B82F6" },
                new Category { CategoryId = 2, Name = "Bug Fix", ColorCode = "#EF4444" },
                new Category { CategoryId = 3, Name = "Design", ColorCode = "#8B5CF6" },
                new Category { CategoryId = 4, Name = "Report", ColorCode = "#10B981" },
                new Category { CategoryId = 5, Name = "Meeting", ColorCode = "#F59E0B" }
            );

            // ===== SEED TAGS =====
            modelBuilder.Entity<Tag>().HasData(
                new Tag { TagId = 1, Name = "Urgent", ColorCode = "#DC2626" },
                new Tag { TagId = 2, Name = "Frontend", ColorCode = "#3B82F6" },
                new Tag { TagId = 3, Name = "Backend", ColorCode = "#10B981" },
                new Tag { TagId = 4, Name = "Database", ColorCode = "#6366F1" },
                new Tag { TagId = 5, Name = "Testing", ColorCode = "#F59E0B" }
            );

            // ===== SEED TASKS =====
            modelBuilder.Entity<TaskItem>().HasData(
                new TaskItem
                {
                    TaskId = 1,
                    Title = "Update User Dashboard",
                    Description = "Redesign the user dashboard with new features including charts and analytics",
                    Priority = "High",
                    Status = "In Progress",
                    CategoryId = 1,
                    CreatedById = 1,
                    AssignedToId = 1,
                    DueDate = DateTime.Now.AddDays(3),
                    CompletionPercentage = 60,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    LastModifiedDate = DateTime.Now
                },
                new TaskItem
                {
                    TaskId = 2,
                    Title = "Fix Login Bug",
                    Description = "Users cannot login with email containing special characters",
                    Priority = "High",
                    Status = "Completed",
                    CategoryId = 2,
                    CreatedById = 1,
                    AssignedToId = 2,
                    DueDate = DateTime.Now.AddDays(-1),
                    CompletionPercentage = 100,
                    CreatedDate = DateTime.Now.AddDays(-8),
                    LastModifiedDate = DateTime.Now.AddDays(-1)
                },
                new TaskItem
                {
                    TaskId = 3,
                    Title = "Generate Monthly Report",
                    Description = "Create comprehensive monthly performance report for management",
                    Priority = "Medium",
                    Status = "In Progress",
                    CategoryId = 4,
                    CreatedById = 4,
                    AssignedToId = 2,
                    DueDate = DateTime.Now.AddDays(5),
                    CompletionPercentage = 40,
                    CreatedDate = DateTime.Now.AddDays(-3),
                    LastModifiedDate = DateTime.Now
                },
                new TaskItem
                {
                    TaskId = 4,
                    Title = "Database Optimization",
                    Description = "Optimize slow database queries and improve overall performance",
                    Priority = "High",
                    Status = "In Progress",
                    CategoryId = 1,
                    CreatedById = 1,
                    AssignedToId = 3,
                    DueDate = DateTime.Now.AddDays(-2),
                    CompletionPercentage = 75,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    LastModifiedDate = DateTime.Now
                },
                new TaskItem
                {
                    TaskId = 5,
                    Title = "Design New Logo",
                    Description = "Create fresh branding with modern logo design for the company",
                    Priority = "Medium",
                    Status = "Completed",
                    CategoryId = 3,
                    CreatedById = 4,
                    AssignedToId = 3,
                    DueDate = DateTime.Now.AddDays(-3),
                    CompletionPercentage = 100,
                    CreatedDate = DateTime.Now.AddDays(-12),
                    LastModifiedDate = DateTime.Now.AddDays(-3)
                },
                new TaskItem
                {
                    TaskId = 6,
                    Title = "Setup CI/CD Pipeline",
                    Description = "Implement continuous integration and deployment pipeline",
                    Priority = "High",
                    Status = "In Progress",
                    CategoryId = 1,
                    CreatedById = 1,
                    AssignedToId = 4,
                    DueDate = DateTime.Now.AddDays(7),
                    CompletionPercentage = 85,
                    CreatedDate = DateTime.Now.AddDays(-6),
                    LastModifiedDate = DateTime.Now
                }
            );

            // ===== SEED TASK-TAGS =====
            modelBuilder.Entity<TaskTag>().HasData(
                new { TaskId = 1, TagId = 1 },
                new { TaskId = 1, TagId = 2 },
                new { TaskId = 2, TagId = 1 },
                new { TaskId = 4, TagId = 3 },
                new { TaskId = 4, TagId = 4 },
                new { TaskId = 6, TagId = 3 }
            );

            // ===== SEED COMMENTS =====
            modelBuilder.Entity<Comment>().HasData(
                new Comment
                {
                    CommentId = 1,
                    TaskId = 1,
                    UserId = 4,
                    Content = "Great progress on the dashboard!",
                    CreatedDate = DateTime.Now.AddDays(-2)
                },
                new Comment
                {
                    CommentId = 2,
                    TaskId = 1,
                    UserId = 1,
                    Content = "Working on the charts integration now",
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new Comment
                {
                    CommentId = 3,
                    TaskId = 4,
                    UserId = 3,
                    Content = "Found the slow query, working on optimization",
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            );

            // ===== SEED NOTIFICATIONS =====
            modelBuilder.Entity<Notification>().HasData(
                new Notification
                {
                    NotificationId = 1,
                    TaskId = 1,
                    UserId = 1,
                    Message = "Task 'Update User Dashboard' assigned to you",
                    IsRead = true,
                    CreatedDate = DateTime.Now.AddDays(-5)
                },
                new Notification
                {
                    NotificationId = 2,
                    TaskId = 4,
                    UserId = 3,
                    Message = "Task 'Database Optimization' is overdue",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                },
                new Notification
                {
                    NotificationId = 3,
                    TaskId = 2,
                    UserId = 2,
                    Message = "Task 'Fix Login Bug' has been completed",
                    IsRead = true,
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            );
        }
    }
}