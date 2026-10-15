using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Application.Services
{
    public class LoanService : ILoanService
    {
        private const int AvailableCopyStatusId = 1;
        private const int LoanedCopyStatusId = 2;
        private const int MaxBooksPerLoan = 2;
        private const decimal DefaultDailyFineRate = 10.00m;

        private const string ActiveStatus = "Active";
        private const string OverdueStatus = "Overdue";
        private const string ReturnedStatus = "Returned";
        private const string CompletedStatus = "Completed";
        private const string PendingFineStatus = "Pending";

        private readonly ILoanRepository _loanRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public LoanService(
            ILoanRepository loanRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _loanRepository = loanRepository;
            _configuration = configuration;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            return await _loanRepository.GetLoanedBooksAsync();
        }

        // With confirm=false, this method only calculates the return preview.
        // With confirm=true, it persists the return and any applicable fine.
        public async Task<ReturnResultDto?> RegisterReturnAsync(uint copyId, bool confirm = false)
        {
            var copy = await _loanRepository.GetCopyByIdAsync(copyId);
            if (copy == null)
            {
                return null;
            }

            if (copy.StatusId != LoanedCopyStatusId)
            {
                return ReturnFailure("La copia no está prestada.");
            }

            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);
            if (loan == null)
            {
                return ReturnFailure("No se encontró un préstamo activo para esta copia.");
            }

            var loanItem = FindPendingLoanItem(loan, copyId);
            if (loanItem == null)
            {
                return ReturnFailure("El ejemplar no está pendiente de devolución.");
            }

            var returnDate = DateTime.Now;
            var overdueDays = CountOverdueBusinessDays(loan.DueDate, returnDate);
            var dailyRate = GetDailyFineRate();
            var amount = dailyRate * overdueDays;
            var hasFine = overdueDays > 0;
            var existingFine = await _loanRepository.GetFineByLoanCopyAsync(loan.Id, copyId);

            if (!confirm)
            {
                return BuildReturnPreview(
                    copyId, loan, loanItem, hasFine, existingFine, amount, overdueDays);
            }

            return await ConfirmReturnAsync(
                copy, loan, loanItem, copyId, returnDate, dailyRate,
                amount, overdueDays, hasFine, existingFine);
        }

        private static Loanitem? FindPendingLoanItem(Loan loan, uint copyId)
        {
            return loan.Loanitems.FirstOrDefault(item =>
                item.CopyId == copyId &&
                (item.Status == ActiveStatus || item.Status == OverdueStatus));
        }

        private static ReturnResultDto ReturnFailure(string message)
        {
            return new ReturnResultDto
            {
                HasFine = false,
                Message = message
            };
        }

        private ReturnResultDto BuildReturnPreview(
            uint copyId,
            Loan loan,
            Loanitem loanItem,
            bool hasFine,
            Fine? existingFine,
            decimal amount,
            int overdueDays)
        {
            if (!hasFine)
            {
                return ReturnFailure("Devolución sin multa.");
            }

            return BuildReturnResult(
                copyId,
                loan,
                loanItem,
                hasFine: true,
                message: existingFine != null
                    ? "Multa existente."
                    : "Próxima devolución generará una multa.",
                amount: existingFine?.Amount ?? amount,
                overdueDays: existingFine != null
                    ? (int)existingFine.OverdueDays
                    : overdueDays);
        }

        private async Task<ReturnResultDto> ConfirmReturnAsync(
            Copy copy,
            Loan loan,
            Loanitem loanItem,
            uint copyId,
            DateTime returnDate,
            decimal dailyRate,
            decimal amount,
            int overdueDays,
            bool hasFine,
            Fine? existingFine)
        {
            await using var transaction = await _loanRepository.BeginTransactionAsync();

            try
            {
                ApplyReturnToEntities(copy, loan, loanItem, returnDate);

                if (hasFine && existingFine == null)
                {
                    var fine = CreateFine(
                        loan.Id, copyId, dailyRate, amount, overdueDays, returnDate);

                    Backend.Domain.Validators.FineValidator.Validate(fine);
                    await _loanRepository.AddFineAsync(fine);
                }

                await _loanRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                return BuildReturnResult(
                    copyId,
                    loan,
                    loanItem,
                    hasFine,
                    hasFine
                        ? "Devolución registrada. Multa generada."
                        : "Devolución registrada correctamente.",
                    hasFine ? existingFine?.Amount ?? amount : 0,
                    hasFine
                        ? existingFine != null
                            ? (int)existingFine.OverdueDays
                            : overdueDays
                        : 0);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void ApplyReturnToEntities(
            Copy copy,
            Loan loan,
            Loanitem loanItem,
            DateTime returnDate)
        {
            copy.StatusId = AvailableCopyStatusId;
            copy.UpdatedAt = returnDate;

            loanItem.ReturnedAt = returnDate;
            loanItem.Status = ReturnedStatus;
            loanItem.UpdatedAt = returnDate;

            loan.Status = DetermineLoanStatus(loan, returnDate);
            loan.UpdatedAt = returnDate;
        }

        private static Fine CreateFine(
            uint loanId,
            uint copyId,
            decimal dailyRate,
            decimal amount,
            int overdueDays,
            DateTime createdAt)
        {
            return new Fine
            {
                LoanId = loanId,
                CopyId = copyId,
                DailyRate = dailyRate,
                OverdueDays = (uint)overdueDays,
                Amount = amount,
                Status = PendingFineStatus,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
        }

        private static ReturnResultDto BuildReturnResult(
            uint copyId,
            Loan loan,
            Loanitem loanItem,
            bool hasFine,
            string message,
            decimal amount,
            int overdueDays)
        {
            return new ReturnResultDto
            {
                HasFine = hasFine,
                Message = message,
                CopyId = (int)copyId,
                Amount = amount,
                OverdueDays = overdueDays,
                BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                BookTitle = loanItem.Copy.Book.Title
            };
        }

        private decimal GetDailyFineRate()
        {
            var configuredRate = _configuration["Fine:DailyRate"];

            if (decimal.TryParse(
                configuredRate,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedRate))
            {
                return parsedRate;
            }

            return DefaultDailyFineRate;
        }

        public async Task<(bool Success, string? Error, int? LoanId)> RegisterLoanAsync(
            uint borrowerId,
            IEnumerable<uint> bookIds,
            uint librarianId,
            int loanDays = 14)
        {
            var bookIdList = bookIds?.ToList() ?? new List<uint>();
            var validationError = ValidateLoanRequest(borrowerId, bookIdList);

            if (validationError != null)
            {
                return (false, validationError, null);
            }

            var transaction = await _loanRepository.BeginTransactionAsync();

            try
            {
                var reservation = await ReserveCopiesAsync(bookIdList);
                if (reservation.UnavailableBookIds.Count > 0)
                {
                    await transaction.RollbackAsync();
                    return (
                        false,
                        $"No hay copias disponibles para: {string.Join(',', reservation.UnavailableBookIds)}",
                        null);
                }

                await _loanRepository.SaveChangesAsync();

                var now = DateTime.Now;
                var loan = CreateLoan(
                    borrowerId, librarianId, reservation.ReservedCopies, loanDays, now);

                await _loanRepository.AddLoanAsync(loan);
                await _loanRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null, (int?)loan.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message, null);
            }
        }

        private static string? ValidateLoanRequest(uint borrowerId, IReadOnlyCollection<uint> bookIds)
        {
            if (borrowerId == 0)
            {
                return "BorrowerId inválido.";
            }

            if (bookIds.Count == 0)
            {
                return "No se han especificado libros para el préstamo.";
            }

            if (bookIds.Count > MaxBooksPerLoan)
            {
                return "No se pueden prestar más de 2 libros a la vez.";
            }

            var duplicateBookIds = bookIds
                .GroupBy(id => id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateBookIds.Count > 0)
            {
                return $"No se pueden agregar libros duplicados: {string.Join(',', duplicateBookIds)}";
            }

            return null;
        }

        private async Task<(List<Copy> ReservedCopies, List<uint> UnavailableBookIds)>
            ReserveCopiesAsync(IEnumerable<uint> bookIds)
        {
            var reservedCopies = new List<Copy>();
            var unavailableBookIds = new List<uint>();

            foreach (var bookId in bookIds)
            {
                var copy = await _loanRepository.GetFirstAvailableCopyByBookIdForUpdateAsync(bookId);

                if (copy == null)
                {
                    unavailableBookIds.Add(bookId);
                    continue;
                }

                copy.StatusId = LoanedCopyStatusId;
                copy.UpdatedAt = DateTime.Now;
                reservedCopies.Add(copy);
            }

            return (reservedCopies, unavailableBookIds);
        }

        private static Loan CreateLoan(
            uint borrowerId,
            uint librarianId,
            IEnumerable<Copy> copies,
            int loanDays,
            DateTime now)
        {
            return new Loan
            {
                BorrowerId = (ushort)borrowerId,
                LibrarianId = (ushort)librarianId,
                LoanDate = now,
                DueDate = AddBusinessDays(now, loanDays),
                Status = ActiveStatus,
                CreatedAt = now,
                UpdatedAt = now,
                Loanitems = copies.Select(copy => new Loanitem
                {
                    CopyId = copy.Id,
                    Status = ActiveStatus,
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList()
            };
        }

        public async Task<IEnumerable<LoanListDto>> GetAllLoansAsync()
        {
            await UpdateLoanStatusesAsync();
            return await _loanRepository.GetAllLoansAsync();
        }

        private async Task UpdateLoanStatusesAsync()
        {
            var now = DateTime.Now;
            var loans = await _loanRepository.GetLoansForStatusUpdateAsync();
            var hasChanges = false;

            foreach (var loan in loans)
            {
                var newStatus = DetermineLoanStatus(loan, now);

                if (loan.Status != newStatus)
                {
                    loan.Status = newStatus;
                    loan.UpdatedAt = now;
                    hasChanges = true;
                }

                hasChanges |= SynchronizeItemStatuses(loan, newStatus, now);
            }

            if (hasChanges)
            {
                await _loanRepository.SaveChangesAsync();
            }
        }

        private static string DetermineLoanStatus(Loan loan, DateTime referenceDate)
        {
            var allReturned = loan.Loanitems.All(item => item.Status == ReturnedStatus);
            if (allReturned)
            {
                return CompletedStatus;
            }

            var hasPendingItems = loan.Loanitems.Any(item =>
                item.Status == ActiveStatus || item.Status == OverdueStatus);
            var dueDatePassed = loan.DueDate.Date < referenceDate.Date;

            return dueDatePassed && hasPendingItems
                ? OverdueStatus
                : ActiveStatus;
        }

        private static bool SynchronizeItemStatuses(
            Loan loan,
            string loanStatus,
            DateTime updatedAt)
        {
            var targetItemStatus = loanStatus switch
            {
                OverdueStatus => OverdueStatus,
                ActiveStatus => ActiveStatus,
                _ => null
            };

            if (targetItemStatus == null)
            {
                return false;
            }

            var sourceItemStatus = targetItemStatus == OverdueStatus
                ? ActiveStatus
                : OverdueStatus;

            var changed = false;
            foreach (var item in loan.Loanitems.Where(item => item.Status == sourceItemStatus))
            {
                item.Status = targetItemStatus;
                item.UpdatedAt = updatedAt;
                changed = true;
            }

            return changed;
        }

        public async Task<LoanDto?> GetLoanByIdAsync(uint id)
        {
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(id);
            return loan == null ? null : MapToDto(loan);
        }

        public Task<bool> RegisterLoanAsync(uint bookId)
        {
            // Legacy overload retained for compatibility with existing callers.
            return Task.FromResult(false);
        }

        public async Task<bool> UpdateLoanAsync(uint id, UpdateLoanDto dto)
        {
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(id);
            if (loan == null)
            {
                return false;
            }

            var now = DateTime.Now;
            loan.DueDate = dto.DueDate;
            loan.Status = dto.Status;
            loan.UpdatedAt = now;

            SynchronizeItemsForManualUpdate(loan, dto.Status, now);
            await _loanRepository.SaveChangesAsync();

            return true;
        }

        private static void SynchronizeItemsForManualUpdate(
            Loan loan,
            string requestedStatus,
            DateTime now)
        {
            var dueDatePassed = loan.DueDate.Date < now.Date;

            if (requestedStatus == OverdueStatus || dueDatePassed)
            {
                UpdateItemStatuses(loan, ActiveStatus, OverdueStatus, now);
                return;
            }

            if (requestedStatus == ActiveStatus && !dueDatePassed)
            {
                UpdateItemStatuses(loan, OverdueStatus, ActiveStatus, now);
            }
        }

        private static void UpdateItemStatuses(
            Loan loan,
            string currentStatus,
            string newStatus,
            DateTime now)
        {
            foreach (var item in loan.Loanitems.Where(item => item.Status == currentStatus))
            {
                item.Status = newStatus;
                item.UpdatedAt = now;
            }
        }

        private static LoanDto MapToDto(Loan loan)
        {
            return new LoanDto
            {
                Id = (int)loan.Id,
                BorrowerId = loan.BorrowerId,
                BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                LibrarianId = loan.LibrarianId,
                LibrarianName = $"{loan.Librarian.FirstName} {loan.Librarian.LastName}".Trim(),
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                Status = loan.Status,
                StatusDisplayName = GetLoanStatusDisplayName(loan.Status),
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt,
                Items = loan.Loanitems.Select(MapLoanItemToDto).ToList()
            };
        }

        private static LoanItemDto MapLoanItemToDto(Loanitem loanItem)
        {
            return new LoanItemDto
            {
                CopyId = (int)loanItem.CopyId,
                InternalCode = loanItem.Copy.InternalCode,
                BookId = (int)loanItem.Copy.BookId,
                BookTitle = loanItem.Copy.Book.Title,
                ReturnedAt = loanItem.ReturnedAt,
                Status = loanItem.Status,
                StatusDisplayName = GetLoanItemStatusDisplayName(loanItem.Status)
            };
        }

        private static string GetLoanStatusDisplayName(string status)
        {
            return status switch
            {
                "Active" => "Activo",
                "Completed" => "Completado",
                "Overdue" => "Vencido",
                "Lost" => "Perdido",
                _ => status
            };
        }

        private static string GetLoanItemStatusDisplayName(string status)
        {
            return status switch
            {
                "Active" => "Activo",
                "Returned" => "Devuelto",
                "Overdue" => "Vencido",
                "Lost" => "Perdido",
                _ => status
            };
        }

        // Counts weekdays after dueDate, through and including returnDate.
        private static int CountOverdueBusinessDays(DateTime dueDate, DateTime returnDate)
        {
            var start = dueDate.Date.AddDays(1);
            var end = returnDate.Date;

            if (end < start)
            {
                return 0;
            }

            var count = 0;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
            }

            return count;
        }

        // Adds N weekdays (Mon-Fri), skipping Saturdays and Sundays.
        private static DateTime AddBusinessDays(DateTime start, int businessDays)
        {
            var date = start;
            var added = 0;

            while (added < businessDays)
            {
                date = date.AddDays(1);
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday)
                {
                    added++;
                }
            }

            return date;
        }
    }
}
