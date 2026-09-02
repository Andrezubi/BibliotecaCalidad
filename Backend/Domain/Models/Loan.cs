using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Loan
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LibrarianId { get; set; }

    public int CopyId { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public decimal FineAmount { get; set; }

    public string FineStatus { get; set; } = null!;

    public DateTime? FinePaidAt { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual User Librarian { get; set; } = null!;

    public virtual ICollection<Renewal> Renewals { get; set; } = new List<Renewal>();

    public virtual User User { get; set; } = null!;
}
