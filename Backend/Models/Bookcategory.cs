using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Bookcategory
{
    public uint BookId { get; set; }

    public ushort CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual User? User { get; set; }
}
