using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class User
{
    public ushort Id { get; set; }

    public int Ci { get; set; }

    public string? Complement { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? SecondLastName { get; set; }

    public string? Phone { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public byte RoleId { get; set; }

    public bool IsLibraryComputerUser { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();

    public virtual ICollection<Bookauthor> Bookauthors { get; set; } = new List<Bookauthor>();

    public virtual ICollection<Bookcategory> Bookcategories { get; set; } = new List<Bookcategory>();

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual ICollection<Copystatus> Copystatuses { get; set; } = new List<Copystatus>();

    public virtual ICollection<Finepayment> FinepaymentLibrarians { get; set; } = new List<Finepayment>();

    public virtual ICollection<Finepayment> FinepaymentUsers { get; set; } = new List<Finepayment>();

    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();

    public virtual ICollection<User> InverseUserNavigation { get; set; } = new List<User>();

    public virtual ICollection<Loan> LoanBorrowers { get; set; } = new List<Loan>();

    public virtual ICollection<Loan> LoanLibrarians { get; set; } = new List<Loan>();

    public virtual ICollection<Loan> LoanUsers { get; set; } = new List<Loan>();

    public virtual ICollection<Loanitem> Loanitems { get; set; } = new List<Loanitem>();

    public virtual ICollection<Loanrequest> LoanrequestProcessedByNavigations { get; set; } = new List<Loanrequest>();

    public virtual ICollection<Loanrequest> LoanrequestReaders { get; set; } = new List<Loanrequest>();

    public virtual ICollection<Loanrequest> LoanrequestUsers { get; set; } = new List<Loanrequest>();

    public virtual ICollection<Loanrequestitem> Loanrequestitems { get; set; } = new List<Loanrequestitem>();

    public virtual ICollection<Notification> NotificationRecipients { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Renewal> RenewalLibrarians { get; set; } = new List<Renewal>();

    public virtual ICollection<Renewal> RenewalUsers { get; set; } = new List<Renewal>();

    public virtual ICollection<Reservation> ReservationReaders { get; set; } = new List<Reservation>();

    public virtual ICollection<Reservation> ReservationUsers { get; set; } = new List<Reservation>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Setting> Settings { get; set; } = new List<Setting>();

    public virtual User? UserNavigation { get; set; }
}
