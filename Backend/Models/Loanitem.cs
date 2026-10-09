using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Loanitem
{
    public uint Id { get; set; }

    public uint LoanId { get; set; }

    public uint CopyId { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual Fine? Fine { get; set; }

    public virtual Loan Loan { get; set; } = null!;

    public virtual User? User { get; set; }
}
