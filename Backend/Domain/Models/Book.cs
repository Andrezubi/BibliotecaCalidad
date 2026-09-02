<<<<<<< HEAD
﻿namespace Backend.Domain.Entities;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int EditionNumber { get; set; }

    public string? ISBN { get; set; }
=======
﻿using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int EditionNumber { get; set; }

    public string? Isbn { get; set; }
>>>>>>> 7a9c614a8c8cd84028d112fdf16f25aa086ca436

    public int? PublicationYear { get; set; }

    public string? Publisher { get; set; }

    public int? PageCount { get; set; }

    public string? Description { get; set; }

<<<<<<< HEAD
    public bool IsActive { get; set; }
=======
    public bool? IsActive { get; set; }
>>>>>>> 7a9c614a8c8cd84028d112fdf16f25aa086ca436

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }
<<<<<<< HEAD
}
=======

    public virtual ICollection<Bookauthor> Bookauthors { get; set; } = new List<Bookauthor>();

    public virtual ICollection<Bookcategory> Bookcategories { get; set; } = new List<Bookcategory>();

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual User? User { get; set; }
}
>>>>>>> 7a9c614a8c8cd84028d112fdf16f25aa086ca436
