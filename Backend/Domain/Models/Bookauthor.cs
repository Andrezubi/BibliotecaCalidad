using System;
using System.Collections.Generic;
namespace Backend.Domain.Models;

public partial class Bookauthor
{
    public int BookId { get; set; }

    public int AuthorId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual Book Book { get; set; } = null!;

    public virtual User? User { get; set; }
}
