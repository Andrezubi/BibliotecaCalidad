namespace Backend.Domain.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int EditionNumber { get; set; }

    public string? ISBN { get; set; }

    public int? PublicationYear { get; set; }

    public string? Publisher { get; set; }

    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    // Relaciones
    public virtual User? User { get; set; }

    public virtual ICollection<Bookauthor> Bookauthors { get; set; }
        = new List<Bookauthor>();

    public virtual ICollection<Bookcategory> Bookcategories { get; set; }
        = new List<Bookcategory>();

    public virtual ICollection<Copy> Copies { get; set; }
        = new List<Copy>();
}