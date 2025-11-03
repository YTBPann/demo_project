using Microsoft.EntityFrameworkCore;

namespace OpenIDApp.Models
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
        public DbSet<ExamPlan> ExamPlans { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User sang UserLogin (1-n)
            modelBuilder.Entity<UserLogin>()
                .HasOne(u => u.User)
                .WithMany(l => l.Logins)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User sang Student (1-1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.UserProfile)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User sang Teacher (1-1)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.UserProfile)
                .WithOne(u => u.TeacherProfile)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Teacher sang Subject (1-n)
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Subjects)
                .HasForeignKey(s => s.TeacherId);

            // Subject sang Exam (1-n)
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.Exams)
                .HasForeignKey(e => e.SubjectId);

            // Room sang Exam (1-n)
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Room)
                .WithMany(r => r.Exams)
                .HasForeignKey(e => e.RoomId);

            // Student sang Exam (n-n)
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

            // ExamPlan
            modelBuilder.Entity<ExamPlan>(e =>
            {
                e.HasKey(x => x.PlanId);
                e.HasIndex(x => x.SubjectId).IsUnique();
                e.HasOne(x => x.Subject)
                    .WithMany()
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Room)
                    .WithMany()
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StudentSubject (bảng enroll SV -> Môn)
            modelBuilder.Entity<StudentSubject>(e =>
            {
                e.ToTable("student_subjects");
                e.HasKey(x => new { x.StudentId, x.SubjectId });
                e.Property(x => x.StudentId).HasColumnName("student_id");
                e.Property(x => x.SubjectId).HasColumnName("subject_id");

                e.HasOne<Student>()
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<Subject>()
                    .WithMany()
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ExamPlanRoom>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.SubjectId, x.DayIndex, x.SlotId, x.RoomId }).IsUnique();
                e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }      
    }
}
