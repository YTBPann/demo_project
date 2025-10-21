using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoOpenID.Models
{
    [Table("users")]
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Picture { get; set; }

        public string Role { get; set; } = "guest";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserLogin> Logins { get; set; } = new List<UserLogin>();

        public Student? StudentProfile { get; set; }

        public Teacher? TeacherProfile { get; set; }
    }
}
