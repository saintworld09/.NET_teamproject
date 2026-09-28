using System.ComponentModel.DataAnnotations;

namespace EventHub.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}