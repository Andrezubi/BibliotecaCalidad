using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Pages.Loans
{
    public class DetailsModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;

        public DetailsModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        public LoanDto? Loan { get; set; }

        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public DateTime EditDueDate { get; set; }

        [BindProperty]
        public string EditStatus { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            try
            {
                Loan = await _loanService.GetLoanByIdAsync(Id);

                if (Loan == null)
                {
                    return NotFound();
                }

                // Cargar valores iniciales para el formulario del modal
                EditDueDate = Loan.DueDate;
                EditStatus = Loan.Status;

                return Page();
            }
            catch
            {
                ErrorMessage = "No se pudo cargar el préstamo.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateLoanAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            var dto = new UpdateLoanDto
            {
                DueDate = EditDueDate,
                Status = EditStatus
            };

            var success = await _loanService.UpdateLoanAsync(Id, dto);

            if (success)
            {
                TempData["Success"] = "Préstamo actualizado correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el préstamo.";
            }

            return RedirectToPage(new { id = Id });
        }
    }
}