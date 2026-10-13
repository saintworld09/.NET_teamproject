
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventHub.Models.ViewModels;

public class CreateEventViewModel
{
    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(150, ErrorMessage = "The title cannot exceed 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event date and time are required.")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Event location is required.")]
    [StringLength(200, ErrorMessage = "The location cannot exceed 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100000.")]
    public int Capacity { get; set; } = 50;

    [Range(1, int.MaxValue, ErrorMessage = "Please select an event category.")]
    public int CategoryId { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}