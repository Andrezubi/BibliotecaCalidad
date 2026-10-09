using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Loanrequest
{
    public uint Id { get; set; }

    public ushort ReaderId { get; set; }

    public DateTime RequestedAt { get; set; }

    public string Status { get; set; } = null!;

    public ushort? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Loanrequestitem> Loanrequestitems { get; set; } = new List<Loanrequestitem>();

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User Reader { get; set; } = null!;

    public virtual User? User { get; set; }
}
