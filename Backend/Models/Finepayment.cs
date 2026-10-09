using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Finepayment
{
    public uint Id { get; set; }

    public uint FineId { get; set; }

    public ushort LibrarianId { get; set; }

    public decimal Amount { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public DateTime PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Fine Fine { get; set; } = null!;

    public virtual User Librarian { get; set; } = null!;

    public virtual User? User { get; set; }
}
