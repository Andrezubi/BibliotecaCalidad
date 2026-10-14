using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly LibraryDbcontext _context;

        public LoanService(ILoanRepository loanRepository, LibraryDbcontext context)
        {
            _loanRepository = loanRepository;
            _context = context;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            return await _loanRepository.GetLoanedBooksAsync();
        }

        public async Task<ReturnResultDto?> RegisterReturnAsync(uint copyId)
        {
            var copy = await _loanRepository.GetCopyByIdAsync(copyId);
            if (copy == null || copy.StatusId != 2) // Debe existir y estar Prestada (Loaned)
                return null;

            var now = DateTime.Now;

            // 1. Cambiar estado de la copia a Disponible (1)
            copy.StatusId = 1;
            copy.UpdatedAt = now;

            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);
            if (loan == null)
            {
                await _loanRepository.SaveChangesAsync();
                return new ReturnResultDto
                {
                    HasFine = false,
                    Message = "Copia marcada como disponible sin préstamo activo asociado."
                };
            }

            var loanItem = loan.Loanitems.FirstOrDefault(li => li.CopyId == copyId && li.Status == "Active");
            if (loanItem != null)
            {
                loanItem.ReturnedAt = now;
                loanItem.Status = "Returned";
                loanItem.UpdatedAt = now;
            }

            // Verificar si todos los libros del préstamo ya fueron devueltos
            if (loan.Loanitems.All(li => li.Status == "Returned"))
            {
                loan.Status = "Completed";
                loan.UpdatedAt = now;
            }

            // 2. Cálculo de multa (si la fecha actual superó DueDate)
            bool hasFine = false;
            int overdueDays = 0;
            decimal amount = 0;
            string? receiptNumber = null;

            if (now.Date > loan.DueDate.Date)
            {
                hasFine = true;
                overdueDays = (now.Date - loan.DueDate.Date).Days;
                decimal dailyRate = 10.00m; // 10 Bs por día
                amount = overdueDays * dailyRate;

                // Crear registro en Fine
                var fine = new Fine
                {
                    LoanId = loan.Id,
                    CopyId = copyId,
                    DailyRate = dailyRate,
                    OverdueDays = (uint)overdueDays,
                    Amount = amount,
                    Status = "Paid", // Se registra como pagada inmediatamente
                    CreatedAt = now
                };
                _context.Fines.Add(fine);
                await _context.SaveChangesAsync(); // Para obtener el fine.Id

                // Generar número de recibo único: REC-AÑO-ID
                receiptNumber = $"REC-{now:yyyyMMdd}-{fine.Id:D4}";

                // Crear registro en Finepayment
                var finePayment = new Finepayment
                {
                    FineId = fine.Id,
                    LibrarianId = loan.LibrarianId,
                    Amount = amount,
                    ReceiptNumber = receiptNumber,
                    PaidAt = now,
                    CreatedAt = now
                };
                _context.Finepayments.Add(finePayment);
            }

            await _loanRepository.SaveChangesAsync();

            return new ReturnResultDto
            {
                HasFine = hasFine,
                Message = hasFine ? "Devolución registrada con multa cobrada." : "Devolución registrada sin multas.",
                ReceiptNumber = receiptNumber,
                Amount = amount,
                OverdueDays = overdueDays,
                BorrowerName = $"{loan.Borrower?.FirstName} {loan.Borrower?.LastName}".Trim(),
                BookTitle = copy.Book?.Title ?? "Libro",
                PaidAt = hasFine ? now : null
            };
        }

        public async Task<bool> RegisterLoanAsync(uint bookId)
        {
            var copy = await _loanRepository.GetFirstAvailableCopyByBookIdAsync(bookId);

            if (copy == null)
                return false;

            copy.StatusId = 2; // Loaned
            copy.UpdatedAt = DateTime.Now;

            await _loanRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<LoanListDto>> GetAllLoansAsync()
        {
            return await _loanRepository.GetAllLoansAsync();
        }

        public async Task<LoanDto?> GetLoanByIdAsync(uint id)
        {
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(id);

            if (loan == null)
                return null;

            return MapToDto(loan);
        }

        private static LoanDto MapToDto(Loan loan)
        {
            var loanDto = new LoanDto
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
                Items = loan.Loanitems.Select(li => new LoanItemDto
                {
                    CopyId = (int)li.CopyId,
                    InternalCode = li.Copy.InternalCode,
                    BookId = (int)li.Copy.BookId,
                    BookTitle = li.Copy.Book.Title,
                    ReturnedAt = li.ReturnedAt,
                    Status = li.Status,
                    StatusDisplayName = GetLoanItemStatusDisplayName(li.Status)
                }).ToList()
            };

            return loanDto;
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
    }
}