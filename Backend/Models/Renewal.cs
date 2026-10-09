using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Renewal
{
    public uint Id { get; set; }

    public uint LoanId { get; set; }

    public ushort LibrarianId { get; set; }

    public DateTime RenewalDate { get; set; }

    public DateTime PreviousDueDate { get; set; }

    public DateTime NewDueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual User Librarian { get; set; } = null!;

    public virtual Loan Loan { get; set; } = null!;

    public virtual User? User { get; set; }
}
