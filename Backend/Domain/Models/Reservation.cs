using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Reservation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CopyId { get; set; }

    public DateOnly ReservationDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
