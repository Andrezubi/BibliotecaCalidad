using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Publisher
{
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual User? User { get; set; }
}
