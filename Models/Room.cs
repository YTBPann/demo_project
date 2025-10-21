using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("rooms")]
    public class Room
    {
        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("capacity")]
        public int Capacity { get; set; }

        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}