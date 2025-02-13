using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Action { get; set; } // e.g., "Login Success", "Login Failure"

        public string IPAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}