using System.ComponentModel.DataAnnotations;

namespace FrontEnd.DTOs;

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
}