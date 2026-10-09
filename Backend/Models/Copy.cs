using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Copy
{
    public uint Id { get; set; }

    public uint BookId { get; set; }

    public string InternalCode { get; set; } = null!;

    public byte StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ICollection<Loanitem> Loanitems { get; set; } = new List<Loanitem>();

    public virtual ICollection<Loanrequestitem> Loanrequestitems { get; set; } = new List<Loanrequestitem>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual Copystatus Status { get; set; } = null!;

    public virtual User? User { get; set; }
}
