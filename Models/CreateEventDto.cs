using System.ComponentModel.DataAnnotations;

namespace EventGrok.Models;

public class CreateEventDto
{
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов")]
    public required string Title { get; set; }
    
    [StringLength(500, ErrorMessage = "Описание события не должно превышать 500 символов")]
    public string Description { get; set; } = string.Empty;

    public required DateTime StartAt { get; set; }

    public required DateTime EndAt { get; set; }
}