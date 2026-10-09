using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Copystatus
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual User? User { get; set; }
}
