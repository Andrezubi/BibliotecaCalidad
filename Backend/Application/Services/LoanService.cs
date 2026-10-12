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
    }
}