using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Reservation
{
    public uint Id { get; set; }

    public ushort ReaderId { get; set; }

    public uint CopyId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual User Reader { get; set; } = null!;

    public virtual User? User { get; set; }
}
