using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FrontEnd.Pages.Loans
{
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
                return new ObjectResult(new { message = "No tienes permisos para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            try
            {
                var result = await _loanService.RegisterReturnWithFineAsync(Id, (uint)copyId, false);

                if (result == null)
                {
                    return BadRequest(new { message = "No se pudo procesar la devolución del ejemplar." });
                }

                // Detectar si la petición es AJAX (fetch/XHR que espera JSON)
                var isAjax = (Request?.Headers != null) &&
                             (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              (Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"].ToString().Contains("application/json")));

                if (isAjax)
                {
                    // Para AJAX devolvemos el resultado del cálculo (cliente mostrará modal y confirmará)
                    return new JsonResult(result);
                }
                if (result.HasFine)
                {
                    LastReturnResult = result;
                    Loan = await _loanService.GetLoanByIdAsync(Id);
                    return Page();
                }

                var confirmResult = await _loanService.RegisterReturnWithFineAsync(Id, (uint)copyId, true);
                if (confirmResult == null) return StatusCode(StatusCodes.Status502BadGateway, new { message = "No se pudo confirmar la devolución." });
                TempData["Success"] = confirmResult.Message ?? "Devolución registrada correctamente.";
                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"OnPostReturnItemAsync error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al procesar la devolución." });
            }
        }

        // Confirmar: persistir la devolución (confirm=true) - now handled by OnPostReturnItemAsync with confirm=true
        // Keeping this for backward compatibility but it delegates to the main handler
        public async Task<IActionResult> OnPostConfirmReturnItemAsync([FromForm] int copyId)
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return new ObjectResult(new { message = "No tienes permisos para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            try
            {
                var result = await _loanService.RegisterReturnWithFineAsync(Id, (uint)copyId, true);

                if (result == null)
                {
                    return BadRequest(new { message = "No se pudo confirmar la devolución del ejemplar." });
                }

                var isAjax = (Request?.Headers != null) &&
                             (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              (Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"].ToString().Contains("application/json")));

                if (isAjax)
                {
                    return new JsonResult(result);
                }

                if (result.HasFine)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        result,
                        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
                    );

                    TempData["ReturnResult"] = json;
                }
                else
                {
                    TempData["Success"] = result.Message ?? "Devolución confirmada.";
                }

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"OnPostConfirmReturnItemAsync error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al confirmar la devolución." });
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
