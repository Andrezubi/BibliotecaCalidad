using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Copy
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string InternalCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual User? User { get; set; }
}
