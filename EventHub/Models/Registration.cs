using System.ComponentModel.DataAnnotations;

namespace EventHub.Models;

public class Registration
{
    public int RegistrationId { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Registered";

    public User? User { get; set; }

    public Event? Event { get; set; }
}