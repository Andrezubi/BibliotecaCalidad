using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Renewal
{
    public int Id { get; set; }

    public int LoanId { get; set; }

    public int LibrarianId { get; set; }

    public DateTime RenewalDate { get; set; }

    public DateTime PreviousDueDate { get; set; }

    public DateTime NewDueDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual User Librarian { get; set; } = null!;

    public virtual Loan Loan { get; set; } = null!;

    public virtual User? User { get; set; }
}
