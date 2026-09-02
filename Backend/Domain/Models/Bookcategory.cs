using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Bookcategory
{
    public int BookId { get; set; }

    public int CategoryId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual User? User { get; set; }
}
