using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Category
{
    public ushort Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Bookcategory> Bookcategories { get; set; } = new List<Bookcategory>();

    public virtual User? User { get; set; }
}
