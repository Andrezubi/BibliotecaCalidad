using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Notification
{
    public uint Id { get; set; }

    public ushort RecipientId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual User Recipient { get; set; } = null!;

    public virtual User? User { get; set; }
}
