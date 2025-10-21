using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("user_logins")]
    public class UserLogin
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Provider { get; set; } = string.Empty;

        public string ProviderId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}