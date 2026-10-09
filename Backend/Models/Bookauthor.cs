using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Bookauthor
{
    public uint BookId { get; set; }

    public uint AuthorId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual Book Book { get; set; } = null!;

    public virtual User? User { get; set; }
}
