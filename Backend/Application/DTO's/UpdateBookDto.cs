using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs;

public class UpdateBookDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EditionNumber { get; set; }

    [StringLength(20)]
    public string? ISBN { get; set; }

    [Range(1000, 2100)]
    public int? PublicationYear { get; set; }

    [StringLength(150)]
    public string? Publisher { get; set; }

    [Range(1, int.MaxValue)]
    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public int? UserId { get; set; }
}