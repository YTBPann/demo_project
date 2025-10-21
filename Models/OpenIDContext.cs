using Microsoft.EntityFrameworkCore;
using DemoOpenID.Models;

namespace DemoOpenID.Models
{
    public class OpenIDContext : DbContext
    {
        public OpenIDContext(DbContextOptions<OpenIDContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<StudentExam> StudentExams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User ↔ UserLogin (1-n)
            modelBuilder.Entity<UserLogin>()
                .HasOne(u => u.User)
                .WithMany(l => l.Logins)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User ↔ Student (1-1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User ↔ Teacher (1-1)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Teacher ↔ Subject (1-n)
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Subjects)
                .HasForeignKey(s => s.TeacherId);

            // Subject ↔ Exam (1-n)
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.Exams)
                .HasForeignKey(e => e.SubjectId);

            // Room ↔ Exam (1-n)
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Room)
                .WithMany(r => r.Exams)
                .HasForeignKey(e => e.RoomId);

            // Student ↔ Exam (n-n)
            modelBuilder.Entity<StudentExam>()
                .HasKey(se => new { se.StudentId, se.ExamId });
            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Student)
                .WithMany(s => s.StudentExams)
                .HasForeignKey(se => se.StudentId);
            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Exam)
                .WithMany(e => e.StudentExams)
                .HasForeignKey(se => se.ExamId);
        }
    }
}
