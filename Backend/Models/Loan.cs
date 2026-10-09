using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Loan
{
    public uint Id { get; set; }

    public uint? LoanRequestId { get; set; }

    public ushort BorrowerId { get; set; }

    public ushort LibrarianId { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual User Borrower { get; set; } = null!;

    public virtual User Librarian { get; set; } = null!;

    public virtual Loanrequest? LoanRequest { get; set; }

    public virtual ICollection<Loanitem> Loanitems { get; set; } = new List<Loanitem>();

    public virtual ICollection<Renewal> Renewals { get; set; } = new List<Renewal>();

    public virtual User? User { get; set; }
}
