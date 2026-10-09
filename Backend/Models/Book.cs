using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Book
{
    public uint Id { get; set; }

    public string Title { get; set; } = null!;

    public ushort EditionNumber { get; set; }

    public string? Isbn { get; set; }

    public ushort? PublicationYear { get; set; }

    public string? Publisher { get; set; }

    public uint? PageCount { get; set; }

    public string? Description { get; set; }

    public string? CoverImage { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Bookauthor> Bookauthors { get; set; } = new List<Bookauthor>();

    public virtual ICollection<Bookcategory> Bookcategories { get; set; } = new List<Bookcategory>();

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual User? User { get; set; }
}
