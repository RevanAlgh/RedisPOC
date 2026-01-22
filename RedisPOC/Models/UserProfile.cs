using System.ComponentModel.DataAnnotations;

namespace RedisPOC.Models
{
    public class UserProfile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
