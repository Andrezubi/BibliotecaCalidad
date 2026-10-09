using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Loanrequestitem
{
    public uint Id { get; set; }

    public uint LoanRequestId { get; set; }

    public uint CopyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual Loanrequest LoanRequest { get; set; } = null!;

    public virtual User? User { get; set; }
}
