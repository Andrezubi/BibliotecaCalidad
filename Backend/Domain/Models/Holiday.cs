using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Holiday
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
