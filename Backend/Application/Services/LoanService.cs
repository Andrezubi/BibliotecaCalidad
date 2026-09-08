using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

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

        public async Task<bool> RegisterReturnAsync(int copyId)
            {
                var copy = await _loanRepository.GetCopyByIdAsync(copyId);

                // La copia debe existir, estar activa y estar prestada.
                if (copy == null || !copy.IsActive)
                    return false;

                if (copy.Status != "Loaned")
                    return false;

                // 1) Cambiar el estado de la copia a Disponible.
                copy.Status = "Available";
                copy.UpdatedAt = DateTime.Now;

                // 2) Registrar la devolución en el préstamo activo, si existe.
                var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);

                if (loan != null)
                {
                    loan.ReturnedAt = DateTime.Now;
                    loan.Status = "Returned";
                    loan.UpdatedAt = DateTime.Now;
                }

                await _loanRepository.SaveChangesAsync();

                return true;
        }
        public async Task<bool> RegisterLoanAsync(int bookId)
        {
            var copy = await _loanRepository.GetFirstAvailableCopyByBookIdAsync(bookId);

            // El libro no tiene ninguna copia disponible.
            if (copy == null)
                return false;

            // Cambiar el estado de la copia a Prestado.
            copy.Status = "Loaned";
            copy.UpdatedAt = DateTime.Now;

            await _loanRepository.SaveChangesAsync();

            return true;
        }
    }
}