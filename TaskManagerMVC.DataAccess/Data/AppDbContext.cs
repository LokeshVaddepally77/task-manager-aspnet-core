using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Models;

namespace TaskManagerMVC.DataAccess.Data
{
    /// <summary>
    /// Application Database Context
    /// Central point for EF Core database operations
    /// Supports multiple providers: SQL Server, MySQL, InMemory
    /// Includes Role-Based Authentication Support
    /// </summary>
    public class AppDbContext : DbContext
    {
        // ==================== CONSTRUCTOR ====================

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ==================== DB SETS (Tables) ====================

        /// <summary>
        /// Users table - with authentication and role support
        /// </summary>
        public DbSet<ApplicationUser> Users { get; set; }

        /// <summary>
        /// Tasks table
        /// </summary>
        public DbSet<TaskItem> Tasks { get; set; }

        /// <summary>
        /// Categories table
        /// </summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>
        /// Tags table
        /// </summary>
        public DbSet<Tag> Tags { get; set; }

        /// <summary>
        /// TaskTag junction table (Many-to-Many)
        /// </summary>
        public DbSet<TaskTag> TaskTags { get; set; }

        /// <summary>
        /// Comments table
        /// </summary>
        public DbSet<Comment> Comments { get; set; }

        /// <summary>
        /// Notifications table
        /// </summary>
        public DbSet<Notification> Notifications { get; set; }

        /// <summary>
        /// Attachments table
        /// </summary>
        public DbSet<Attachment> Attachments { get; set; }

        /// <summary>
        /// TaskHistory (Audit Log) table
        /// </summary>
        public DbSet<TaskHistory> TaskHistories { get; set; }

        public DbSet<Role> Roles { get; set; }

        // ==================== MODEL CONFIGURATION ====================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== CONFIGURE USER UNIQUE CONSTRAINTS =====
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ===== CONFIGURE COMPOSITE PRIMARY KEY FOR TASKTAG =====
            modelBuilder.Entity<TaskTag>()
                .HasKey(tt => new { tt.TaskId, tt.TagId });

            // ===== CONFIGURE TASK-TAG MANY-TO-MANY RELATIONSHIP =====
            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.Task)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE USER-TASK RELATIONSHIPS =====
            // User creates tasks
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // User is assigned tasks
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== CONFIGURE TASK-CATEGORY RELATIONSHIP =====
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.CategoryNavigation)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== CONFIGURE TASK-COMMENT RELATIONSHIP =====
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE TASK-NOTIFICATION RELATIONSHIP =====
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Task)
                .WithMany(t => t.Notifications)
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE TASK-ATTACHMENT RELATIONSHIP =====
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE TASK-HISTORY RELATIONSHIP =====
            modelBuilder.Entity<TaskHistory>()
                .HasOne(h => h.Task)
                .WithMany(t => t.TaskHistories)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE USER-NOTIFICATION RELATIONSHIP =====
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== CONFIGURE USER-COMMENT RELATIONSHIP =====
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add to OnModelCreating method - Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Manager", Description = "Full access to all features", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new Role { RoleId = 2, RoleName = "Employee", Description = "Can view and update assigned tasks", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) }
            );

            // ===== SEED DATA =====
            modelBuilder.Seed();
        }
    }
}