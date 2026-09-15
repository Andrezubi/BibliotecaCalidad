using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs;

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;

    public int? EditionNumber { get; set; }

    public string ISBN { get; set; } = string.Empty;

    public int? PublicationYear { get; set; }

    public string Publisher { get; set; } = string.Empty;

    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public int? UserId { get; set; }
    // Ruta de la imagen de portada
    public IFormFile? CoverImage { get; set; }


    // Existing authors
    public List<int> AuthorIds { get; set; } = new();

    // Existing categories
    public List<int> CategoryIds { get; set; } = new();

}