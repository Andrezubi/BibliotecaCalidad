using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FrontEnd.Pages.Loans
{
    [IgnoreAntiforgeryToken]
    public class DetailsModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;

        public DetailsModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        // Resultado de la última devolución (cuando hay multa) para renderizar el modal sin depender de TempData
        public FineReceiptDto? LastReturnResult { get; set; }

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

        // Preview: calcula multa sin persistir (confirm=false)
        public async Task<IActionResult> OnPostReturnItemAsync([FromForm] int copyId, [FromQuery] bool confirm = false)
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return new JsonResult(new { message = "No tienes permisos para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            try
            {
                var result = await _loanService.RegisterReturnWithFineAsync((uint)copyId, confirm);

                if (result == null)
                    return BadRequest(new { message = "No se pudo procesar la devolución del ejemplar." });

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"OnPostReturnItemAsync error: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Confirmar: persistir la devolución (confirm=true) - now handled by OnPostReturnItemAsync with confirm=true
        // Keeping this for backward compatibility but it delegates to the main handler
        public async Task<IActionResult> OnPostConfirmReturnItemAsync([FromForm] int copyId)
        {
            return await OnPostReturnItemAsync(copyId, true);
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