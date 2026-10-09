using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Fine
{
    public uint Id { get; set; }

    public uint LoanItemId { get; set; }

    public decimal DailyRate { get; set; }

    public uint OverdueDays { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Finepayment> Finepayments { get; set; } = new List<Finepayment>();

    public virtual Loanitem LoanItem { get; set; } = null!;

    public virtual User? User { get; set; }
}
