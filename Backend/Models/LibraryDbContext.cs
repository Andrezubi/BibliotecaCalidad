using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Backend.Models;

public partial class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Bookauthor> Bookauthors { get; set; }

    public virtual DbSet<Bookcategory> Bookcategories { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Copy> Copies { get; set; }

    public virtual DbSet<Copystatus> Copystatuses { get; set; }

    public virtual DbSet<Fine> Fines { get; set; }

    public virtual DbSet<Finepayment> Finepayments { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<Loanitem> Loanitems { get; set; }

    public virtual DbSet<Loanrequest> Loanrequests { get; set; }

    public virtual DbSet<Loanrequestitem> Loanrequestitems { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Renewal> Renewals { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=librarydb2;user=root;password=1234", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.46-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("author");

            entity.HasIndex(e => e.UserId, "IX_Author_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.SecondLastName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Authors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Author_User");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("book");

            entity.HasIndex(e => e.EditionNumber, "IX_Book_EditionNumber");

            entity.HasIndex(e => e.Title, "IX_Book_Title");

            entity.HasIndex(e => e.UserId, "IX_Book_UserId");

            entity.HasIndex(e => e.Isbn, "UQ_Book_ISBN").IsUnique();

            entity.Property(e => e.CoverImage).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .HasColumnName("ISBN");
            entity.Property(e => e.Publisher).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Books)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Book_User");
        });

        modelBuilder.Entity<Bookauthor>(entity =>
        {
            entity.HasKey(e => new { e.BookId, e.AuthorId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("bookauthor");

            entity.HasIndex(e => e.AuthorId, "IX_BookAuthor_AuthorId");

            entity.HasIndex(e => e.UserId, "IX_BookAuthor_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Author).WithMany(p => p.Bookauthors)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK_BookAuthor_Author");

            entity.HasOne(d => d.Book).WithMany(p => p.Bookauthors)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK_BookAuthor_Book");

            entity.HasOne(d => d.User).WithMany(p => p.Bookauthors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BookAuthor_User");
        });

        modelBuilder.Entity<Bookcategory>(entity =>
        {
            entity.HasKey(e => new { e.BookId, e.CategoryId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("bookcategory");

            entity.HasIndex(e => e.CategoryId, "IX_BookCategory_CategoryId");

            entity.HasIndex(e => e.UserId, "IX_BookCategory_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Book).WithMany(p => p.Bookcategories)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK_BookCategory_Book");

            entity.HasOne(d => d.Category).WithMany(p => p.Bookcategories)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_BookCategory_Category");

            entity.HasOne(d => d.User).WithMany(p => p.Bookcategories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BookCategory_User");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("category");

            entity.HasIndex(e => e.UserId, "IX_Category_UserId");

            entity.HasIndex(e => e.Name, "UQ_Category_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Categories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Category_User");
        });

        modelBuilder.Entity<Copy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("copy");

            entity.HasIndex(e => e.BookId, "IX_Copy_BookId");

            entity.HasIndex(e => e.StatusId, "IX_Copy_StatusId");

            entity.HasIndex(e => e.UserId, "IX_Copy_UserId");

            entity.HasIndex(e => e.InternalCode, "UQ_Copy_InternalCode").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.InternalCode).HasMaxLength(50);
            entity.Property(e => e.StatusId).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Book).WithMany(p => p.Copies)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Copy_Book");

            entity.HasOne(d => d.Status).WithMany(p => p.Copies)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Copy_Status");

            entity.HasOne(d => d.User).WithMany(p => p.Copies)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Copy_User");
        });

        modelBuilder.Entity<Copystatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("copystatus");

            entity.HasIndex(e => e.UserId, "IX_CopyStatus_UserId");

            entity.HasIndex(e => e.Name, "UQ_CopyStatus_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Copystatuses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CopyStatus_User");
        });

        modelBuilder.Entity<Fine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("fine");

            entity.HasIndex(e => e.Status, "IX_Fine_Status");

            entity.HasIndex(e => e.UserId, "IX_Fine_UserId");

            entity.HasIndex(e => e.LoanItemId, "UQ_Fine_LoanItem").IsUnique();

            entity.Property(e => e.Amount).HasPrecision(7, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DailyRate)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'10.00'");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','PartiallyPaid','Paid','Waived')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.LoanItem).WithOne(p => p.Fine)
                .HasForeignKey<Fine>(d => d.LoanItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fine_LoanItem");

            entity.HasOne(d => d.User).WithMany(p => p.Fines)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Fine_User");
        });

        modelBuilder.Entity<Finepayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("finepayment");

            entity.HasIndex(e => e.FineId, "IX_FinePayment_FineId");

            entity.HasIndex(e => e.LibrarianId, "IX_FinePayment_LibrarianId");

            entity.HasIndex(e => e.PaidAt, "IX_FinePayment_PaidAt");

            entity.HasIndex(e => e.UserId, "IX_FinePayment_UserId");

            entity.HasIndex(e => e.ReceiptNumber, "UQ_FinePayment_ReceiptNumber").IsUnique();

            entity.Property(e => e.Amount).HasPrecision(7, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ReceiptNumber).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Fine).WithMany(p => p.Finepayments)
                .HasForeignKey(d => d.FineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinePayment_Fine");

            entity.HasOne(d => d.Librarian).WithMany(p => p.FinepaymentLibrarians)
                .HasForeignKey(d => d.LibrarianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinePayment_Librarian");

            entity.HasOne(d => d.User).WithMany(p => p.FinepaymentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FinePayment_User");
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("loan");

            entity.HasIndex(e => e.BorrowerId, "IX_Loan_BorrowerId");

            entity.HasIndex(e => e.DueDate, "IX_Loan_DueDate");

            entity.HasIndex(e => e.LibrarianId, "IX_Loan_LibrarianId");

            entity.HasIndex(e => e.LoanRequestId, "IX_Loan_LoanRequestId");

            entity.HasIndex(e => e.Status, "IX_Loan_Status");

            entity.HasIndex(e => e.UserId, "IX_Loan_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.LoanDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Completed','Overdue','Lost')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Borrower).WithMany(p => p.LoanBorrowers)
                .HasForeignKey(d => d.BorrowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loan_Borrower");

            entity.HasOne(d => d.Librarian).WithMany(p => p.LoanLibrarians)
                .HasForeignKey(d => d.LibrarianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loan_Librarian");

            entity.HasOne(d => d.LoanRequest).WithMany(p => p.Loans)
                .HasForeignKey(d => d.LoanRequestId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Loan_LoanRequest");

            entity.HasOne(d => d.User).WithMany(p => p.LoanUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Loan_User");
        });

        modelBuilder.Entity<Loanitem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("loanitem");

            entity.HasIndex(e => e.CopyId, "IX_LoanItem_CopyId");

            entity.HasIndex(e => e.Status, "IX_LoanItem_Status");

            entity.HasIndex(e => e.UserId, "IX_LoanItem_UserId");

            entity.HasIndex(e => new { e.LoanId, e.CopyId }, "UQ_LoanItem_Loan_Copy").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ReturnedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Returned','Overdue','Lost')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Copy).WithMany(p => p.Loanitems)
                .HasForeignKey(d => d.CopyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanItem_Copy");

            entity.HasOne(d => d.Loan).WithMany(p => p.Loanitems)
                .HasForeignKey(d => d.LoanId)
                .HasConstraintName("FK_LoanItem_Loan");

            entity.HasOne(d => d.User).WithMany(p => p.Loanitems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LoanItem_User");
        });

        modelBuilder.Entity<Loanrequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("loanrequest");

            entity.HasIndex(e => e.ProcessedBy, "IX_LoanRequest_ProcessedBy");

            entity.HasIndex(e => e.ReaderId, "IX_LoanRequest_ReaderId");

            entity.HasIndex(e => e.RequestedAt, "IX_LoanRequest_RequestedAt");

            entity.HasIndex(e => e.Status, "IX_LoanRequest_Status");

            entity.HasIndex(e => e.UserId, "IX_LoanRequest_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Approved','Rejected','Completed','Cancelled')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.LoanrequestProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LoanRequest_ProcessedBy");

            entity.HasOne(d => d.Reader).WithMany(p => p.LoanrequestReaders)
                .HasForeignKey(d => d.ReaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanRequest_Reader");

            entity.HasOne(d => d.User).WithMany(p => p.LoanrequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LoanRequest_User");
        });

        modelBuilder.Entity<Loanrequestitem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("loanrequestitem");

            entity.HasIndex(e => e.CopyId, "IX_LoanRequestItem_CopyId");

            entity.HasIndex(e => e.UserId, "IX_LoanRequestItem_UserId");

            entity.HasIndex(e => new { e.LoanRequestId, e.CopyId }, "UQ_LoanRequestItem_Request_Copy").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Copy).WithMany(p => p.Loanrequestitems)
                .HasForeignKey(d => d.CopyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanRequestItem_Copy");

            entity.HasOne(d => d.LoanRequest).WithMany(p => p.Loanrequestitems)
                .HasForeignKey(d => d.LoanRequestId)
                .HasConstraintName("FK_LoanRequestItem_Request");

            entity.HasOne(d => d.User).WithMany(p => p.Loanrequestitems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LoanRequestItem_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notification");

            entity.HasIndex(e => e.CreatedAt, "IX_Notification_CreatedAt");

            entity.HasIndex(e => e.IsRead, "IX_Notification_IsRead");

            entity.HasIndex(e => e.RecipientId, "IX_Notification_RecipientId");

            entity.HasIndex(e => e.UserId, "IX_Notification_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Recipient).WithMany(p => p.NotificationRecipients)
                .HasForeignKey(d => d.RecipientId)
                .HasConstraintName("FK_Notification_Recipient");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<Renewal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("renewal");

            entity.HasIndex(e => e.LibrarianId, "IX_Renewal_LibrarianId");

            entity.HasIndex(e => e.LoanId, "IX_Renewal_LoanId");

            entity.HasIndex(e => e.UserId, "IX_Renewal_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.NewDueDate).HasColumnType("datetime");
            entity.Property(e => e.PreviousDueDate).HasColumnType("datetime");
            entity.Property(e => e.RenewalDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Librarian).WithMany(p => p.RenewalLibrarians)
                .HasForeignKey(d => d.LibrarianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Renewal_Librarian");

            entity.HasOne(d => d.Loan).WithMany(p => p.Renewals)
                .HasForeignKey(d => d.LoanId)
                .HasConstraintName("FK_Renewal_Loan");

            entity.HasOne(d => d.User).WithMany(p => p.RenewalUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Renewal_User");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reservation");

            entity.HasIndex(e => e.CopyId, "IX_Reservation_CopyId");

            entity.HasIndex(e => e.EndAt, "IX_Reservation_EndAt");

            entity.HasIndex(e => e.ReaderId, "IX_Reservation_ReaderId");

            entity.HasIndex(e => e.StartAt, "IX_Reservation_StartAt");

            entity.HasIndex(e => e.Status, "IX_Reservation_Status");

            entity.HasIndex(e => e.UserId, "IX_Reservation_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.EndAt).HasColumnType("datetime");
            entity.Property(e => e.StartAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Active','Completed','Cancelled','Expired')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Copy).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CopyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservation_Copy");

            entity.HasOne(d => d.Reader).WithMany(p => p.ReservationReaders)
                .HasForeignKey(d => d.ReaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservation_Reader");

            entity.HasOne(d => d.User).WithMany(p => p.ReservationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Reservation_User");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("role");

            entity.HasIndex(e => e.UserId, "IX_Role_UserId");

            entity.HasIndex(e => e.Name, "UQ_Role_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Roles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Role_User");
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("setting");

            entity.HasIndex(e => e.UserId, "IX_Setting_UserId");

            entity.HasIndex(e => e.SettingKey, "UQ_Setting_Key").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.SettingKey).HasMaxLength(50);
            entity.Property(e => e.SettingValue).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Settings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Setting_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user");

            entity.HasIndex(e => e.RoleId, "IX_User_RoleId");

            entity.HasIndex(e => e.UserId, "IX_User_UserId");

            entity.HasIndex(e => new { e.Ci, e.Complement }, "UQ_User_CI_Complement").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_User_Username").IsUnique();

            entity.Property(e => e.Ci).HasColumnName("CI");
            entity.Property(e => e.Complement).HasMaxLength(2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.SecondLastName).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Blocked','Inactive')");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.InverseUserNavigation)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_User_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
