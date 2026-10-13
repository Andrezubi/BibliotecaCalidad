using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Models;

namespace Backend.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;

        public LoanService(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            return await _loanRepository.GetLoanedBooksAsync();
        }

        public async Task<bool> RegisterReturnAsync(uint copyId)
        {
            var copy = await _loanRepository.GetCopyByIdAsync(copyId);

            // La copia debe existir y estar prestada.
            if (copy == null)
                return false;

            if (copy.StatusId != 2) // Loaned
                return false;

            // 1) Cambiar el estado de la copia a Disponible.
            copy.StatusId = 1; // Available
            copy.UpdatedAt = DateTime.Now;

            // 2) Registrar la devolución en el préstamo activo, si existe.
            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);

            if (loan != null)
            {
                var loanItem = loan.Loanitems.FirstOrDefault(li => li.CopyId == copyId && li.Status == "Active");
                if (loanItem != null)
                {
                    loanItem.ReturnedAt = DateTime.Now;
                    loanItem.Status = "Returned";
                    loanItem.UpdatedAt = DateTime.Now;
                }
                
                // Check if all items are returned
                if (loan.Loanitems.All(li => li.Status == "Returned"))
                {
                    loan.Status = "Completed";
                    loan.UpdatedAt = DateTime.Now;
                }
            }

            await _loanRepository.SaveChangesAsync();

            return true;
        }
        public async Task<bool> RegisterLoanAsync(uint bookId)
        {
            var copy = await _loanRepository.GetFirstAvailableCopyByBookIdAsync(bookId);

            // El libro no tiene ninguna copia disponible.
            if (copy == null)
                return false;

            // Cambiar el estado de la copia a Prestado.
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