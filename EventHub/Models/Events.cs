using System.ComponentModel.DataAnnotations;

namespace EventHub.Models;

public class Event
{
    public int EventId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int Capacity { get; set; }

    public int CategoryId { get; set; }

    public int OrganizerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }

    public User? Organizer { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}