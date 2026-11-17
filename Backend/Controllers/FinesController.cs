using System.Security.Claims;
using Backend.Application.Services;
using Backend.Infrastructure.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/fines")]
[Authorize(Roles = "Admin,Librarian")]
public sealed class FinesController : ControllerBase
{
    private readonly FineManagementService _service;
    public FinesController(FineManagementService service) => _service = service;

    [HttpGet("loans")]
    public async Task<IActionResult> GetSummaries() =>
        Ok(await _service.GetLoanSummariesAsync());

    [HttpGet("loans/{loanId:int}")]
    public async Task<IActionResult> GetLoanFines(int loanId)
    {
        var details = await _service.GetLoanFinesAsync(loanId);
        return details == null ? NotFound() : Ok(details);
    }

    [HttpPost("{fineId:int}/pay")]
    public async Task<IActionResult> Pay(int fineId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ushort.TryParse(userId, out var librarianId) || librarianId == 0)
            return Unauthorized(new { message = "No se pudo identificar al bibliotecario." });

        try
        {
            var receipt = await _service.PayFineAsync(fineId, librarianId);
            return Ok(receipt);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("payments/{paymentId:int}/receipt")]
    public async Task<IActionResult> DownloadReceipt(int paymentId)
    {
        var receipt = await _service.GetReceiptAsync(paymentId);
        if (receipt == null) return NotFound();
        var pdf = FineReceiptPdfGenerator.Generate(receipt);
        return File(pdf, "application/pdf", $"Recibo-{receipt.ReceiptNumber}.pdf");
    }
}
