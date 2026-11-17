using System.Data;
using System.Globalization;
using Backend.Application.DTOs;
using Backend.Domain.Validators;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Services;

public sealed class FineManagementService
{
    private readonly LibraryDbcontext _db;
    private readonly IConfiguration _configuration;

    public FineManagementService(LibraryDbcontext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<List<LoanFineSummaryDto>> GetLoanSummariesAsync()
    {
        var fines = await _db.Fines.AsNoTracking()
            .Include(f => f.Finepayments)
            .ToListAsync();

        return fines.GroupBy(f => f.LoanId).Select(group =>
        {
            var payments = group.SelectMany(f => f.Finepayments).ToList();
            return new LoanFineSummaryDto
            {
                LoanId = checked((int)group.Key),
                FineCount = group.Count(),
                TotalAmount = group.Sum(f => f.Amount),
                PaidAmount = payments.Sum(p => p.Amount),
                PendingAmount = group.Sum(f => f.Status == "Waived"
                    ? 0m
                    : Math.Max(0m, f.Amount - f.Finepayments.Sum(p => p.Amount))),
                LastPaymentId = payments.OrderByDescending(p => p.PaidAt)
                    .ThenByDescending(p => p.Id)
                    .Select(p => (int?)checked((int)p.Id)).FirstOrDefault()
            };
        }).ToList();
    }

    public async Task<LoanFineDetailDto?> GetLoanFinesAsync(int loanId)
    {
        if (loanId <= 0) return null;

        var loan = await _db.Loans.AsNoTracking()
            .Include(l => l.Loanitems)
                .ThenInclude(li => li.Copy)
                    .ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(l => l.Id == (uint)loanId);

        if (loan == null) return null;

        var storedFines = await _db.Fines.AsNoTracking()
            .Include(f => f.Finepayments)
            .Include(f => f.Loanitem)
                .ThenInclude(li => li.Copy)
                    .ThenInclude(c => c.Book)
            .Where(f => f.LoanId == (uint)loanId)
            .OrderBy(f => f.Id)
            .ToListAsync();

        var result = new LoanFineDetailDto { LoanId = loanId };
        foreach (var fine in storedFines)
        {
            var paid = fine.Finepayments.Sum(p => p.Amount);
            var pending = fine.Status == "Waived"
                ? 0m : Math.Max(0m, fine.Amount - paid);

            result.Fines.Add(new LoanFineItemDto
            {
                FineId = checked((int)fine.Id),
                BookTitle = fine.Loanitem.Copy.Book.Title,
                OverdueDays = checked((int)fine.OverdueDays),
                DailyRate = fine.DailyRate,
                Amount = fine.Amount,
                PaidAmount = paid,
                PendingAmount = pending,
                Status = fine.Status,
                Payments = fine.Finepayments.OrderBy(p => p.PaidAt)
                    .Select(p => new FinePaymentInfoDto
                    {
                        PaymentId = checked((int)p.Id),
                        ReceiptNumber = p.ReceiptNumber,
                        Amount = p.Amount,
                        PaidAt = p.PaidAt
                    }).ToList()
            });
        }

        // Los libros aún no devueltos no tienen multa registrada: solo mostrar estimación.
        // La misma regla de días hábiles se utiliza en LoanService.RegisterReturnAsync.
        var existingCopyIds = storedFines.Select(f => f.CopyId).ToHashSet();
        var dailyRate = GetDailyRate();
        var days = CountOverdueBusinessDays(loan.DueDate, DateTime.Now);
        if (days > 0)
        {
            foreach (var item in loan.Loanitems.Where(li =>
                (li.Status == "Active" || li.Status == "Overdue") &&
                !existingCopyIds.Contains(li.CopyId)))
            {
                result.Fines.Add(new LoanFineItemDto
                {
                    FineId = 0,
                    BookTitle = item.Copy.Book.Title,
                    OverdueDays = days,
                    DailyRate = dailyRate,
                    Amount = dailyRate * days,
                    Status = "Estimated"
                });
            }
        }

        result.TotalRegistered = result.Fines.Where(f => f.FineId > 0).Sum(f => f.Amount);
        result.TotalPaid = result.Fines.Sum(f => f.PaidAmount);
        result.TotalPending = result.Fines.Sum(f => f.PendingAmount);
        result.TotalEstimated = result.Fines.Where(f => f.FineId == 0).Sum(f => f.Amount);
        return result;
    }

    // Solo cobra el saldo completo de una multa. El importe nunca llega desde el cliente.
    public async Task<FinePaymentReceiptDto> PayFineAsync(int fineId, ushort librarianId)
    {
        if (fineId <= 0 || librarianId == 0)
            throw new InvalidOperationException("La multa o el bibliotecario no es válido.");

        // Serializable impide que dos cobros simultáneos confirmen el mismo saldo.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var fine = await _db.Fines
            .Include(f => f.Finepayments)
            .FirstOrDefaultAsync(f => f.Id == (uint)fineId);

        if (fine == null)
            throw new KeyNotFoundException("No se encontró la multa.");
        if (fine.Status == "Waived")
            throw new InvalidOperationException("Una multa condonada no puede cobrarse.");

        var balance = fine.Amount - fine.Finepayments.Sum(p => p.Amount);
        if (fine.Status == "Paid" || balance <= 0)
            throw new InvalidOperationException("La multa ya está pagada.");

        var now = DateTime.Now;
        var payment = new Finepayment
        {
            FineId = fine.Id,
            LibrarianId = librarianId,
            UserId = librarianId,
            Amount = balance,
            PaidAt = now,
            CreatedAt = now,
            ReceiptNumber = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 30).ToUpperInvariant()
        };

        FinePaymentValidator.Validate(payment);
        _db.Finepayments.Add(payment);
        fine.Status = "Paid";
        fine.UpdatedAt = now;
        fine.UserId = librarianId;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (await GetReceiptAsync(checked((int)payment.Id)))!;
    }

    public async Task<FinePaymentReceiptDto?> GetReceiptAsync(int paymentId)
    {
        if (paymentId <= 0) return null;
        var payment = await _db.Finepayments.AsNoTracking()
            .Include(p => p.Librarian)
            .Include(p => p.Fine)
                .ThenInclude(f => f.Loanitem)
                    .ThenInclude(li => li.Copy)
                        .ThenInclude(c => c.Book)
            .Include(p => p.Fine)
                .ThenInclude(f => f.Loanitem)
                    .ThenInclude(li => li.Loan)
                        .ThenInclude(l => l.Borrower)
            .FirstOrDefaultAsync(p => p.Id == (uint)paymentId);

        if (payment == null) return null;
        var borrower = payment.Fine.Loanitem.Loan.Borrower;
        return new FinePaymentReceiptDto
        {
            PaymentId = checked((int)payment.Id),
            ReceiptNumber = payment.ReceiptNumber,
            PaidAt = payment.PaidAt,
            Amount = payment.Amount,
            BorrowerName = $"{borrower.FirstName} {borrower.LastName}".Trim(),
            BookTitle = payment.Fine.Loanitem.Copy.Book.Title,
            LibrarianName = $"{payment.Librarian.FirstName} {payment.Librarian.LastName}".Trim(),
            OverdueDays = checked((int)payment.Fine.OverdueDays),
            DailyRate = payment.Fine.DailyRate
        };
    }

    private decimal GetDailyRate()
    {
        return decimal.TryParse(_configuration["Fine:DailyRate"],
            NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)
            ? rate : 10m;
    }

    private static int CountOverdueBusinessDays(DateTime dueDate, DateTime currentDate)
    {
        var count = 0;
        for (var date = dueDate.Date.AddDays(1); date <= currentDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }
}
