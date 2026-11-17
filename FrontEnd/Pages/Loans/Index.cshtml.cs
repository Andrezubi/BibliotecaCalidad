using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Pages.Loans;

public class IndexModel : AuthorizedPageModel
{
    private readonly LoanService _loanService;
    private readonly FinePaymentIdProtector _paymentIdProtector;

    public IndexModel(LoanService loanService, FinePaymentIdProtector paymentIdProtector)
    {
        _loanService = loanService;
        _paymentIdProtector = paymentIdProtector;
    }

    public List<LoanListDto> Loans { get; set; } = new();
    public Dictionary<int, LoanFineSummaryDto> FineSummaries { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FineError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsInAnyRole("Admin", "Librarian"))
            return RedirectToPage("/AccessDenied");

        try
        {
            Loans = await _loanService.GetAllLoansAsync();
        }
        catch
        {
            ErrorMessage = "No se pudieron cargar los préstamos.";
            return Page();
        }

        try
        {
            FineSummaries = (await _loanService.GetFineSummariesAsync())
                .ToDictionary(f => f.LoanId);
        }
        catch
        {
            FineError = "No se pudo consultar el estado de las multas.";
        }
        return Page();
    }

    public async Task<IActionResult> OnGetReceiptAsync(string receipt)
    {
        if (!IsInAnyRole("Admin", "Librarian"))
            return RedirectToPage("/AccessDenied");
        if (!_paymentIdProtector.TryUnprotect(receipt, out var paymentId))
            return NotFound();

        try
        {
            var bytes = await _loanService.GetFineReceiptPdfAsync(paymentId);
            return File(bytes, "application/pdf", "Recibo-Multa.pdf");
        }
        catch (HttpRequestException)
        {
            return NotFound("No se encontró el recibo de pago.");
        }
    }
}
