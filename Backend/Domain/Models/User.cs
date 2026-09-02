using System;
using System.Collections.Generic;

namespace Backend.Domain.Models;

public partial class User
{
    public int Id { get; set; }

    public int Ci { get; set; }

    public string? Complement { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();

    public virtual ICollection<Bookauthor> Bookauthors { get; set; } = new List<Bookauthor>();

    public virtual ICollection<Bookcategory> Bookcategories { get; set; } = new List<Bookcategory>();

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();

    public virtual ICollection<Loan> LoanLibrarians { get; set; } = new List<Loan>();

    public virtual ICollection<Loan> LoanUsers { get; set; } = new List<Loan>();

    public virtual ICollection<Renewal> RenewalLibrarians { get; set; } = new List<Renewal>();

    public virtual ICollection<Renewal> RenewalUsers { get; set; } = new List<Renewal>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual Role Role { get; set; } = null!;
}
