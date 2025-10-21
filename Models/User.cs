namespace OpenIDApp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string GoogleId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Picture { get; set; } = "";
        public string Role { get; set; } = "guest";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
