namespace Backend.Application.DTOs;

public class BookDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? EditionNumber { get; set; }

    public string? ISBN { get; set; }

    public int? PublicationYear { get; set; }

    public string? Publisher { get; set; }

    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public string? CoverImage { get; set; }
    // Número de copias disponibles
    public int AvailableCopies { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public List<int> AuthorIds { get; set; } = new();

    public List<int> CategoryIds { get; set; } = new();
}